using JosephM.Xrm.ContentTemplates.Plugins.Localisation;
using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Schema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JosephM.Xrm.ContentTemplates.Plugins.Services
{
    /// <summary>
    /// A service class to populated of templated html with content from records
    /// </summary>
    public class JosephMContentTemplatesService
    {
        //constants for supported tags
        private const string staticIdentifier = "static|";
        private const string ifIdentifier = "if|";
        private const string endifIdentifier = "endif";
        private const string forfetchIdentifier = "forfetch|";
        private const string endforfetchIdentifier = "endforfetch";

        private XrmService XrmService { get; set; }
        public LocalisationService LocalisationService { get; set; }

        public JosephMContentTemplatesService(XrmService xrmService, LocalisationService localisationService)
        {
            XrmService = xrmService;
            LocalisationService = localisationService;
        }

        public GenerateContentResponse GenerateForContentTemplate(Guid contentTemplateId, string emailTemplateTargetType, Guid emailTemplateTargetId, LocalisationService localisationService, Dictionary<string, string> explicitTokenDictionary = null)
        {
            var resource = XrmService.Retrieve(Entities.jmcg_contenttemplate, contentTemplateId);
            var contentDescription = resource.GetStringField(Fields.jmcg_contenttemplate_.jmcg_content);
            var contentSubject = resource.GetStringField(Fields.jmcg_contenttemplate_.jmcg_subject);
            var appendTemplateId = resource.GetLookupGuid(Fields.jmcg_contenttemplate_.jmcg_appendtemplate);

            var response = PopulateTemplateContent(emailTemplateTargetType, new[] { emailTemplateTargetId }, localisationService, explicitTokenDictionary, contentDescription, contentSubject);

            if (appendTemplateId.HasValue)
            {
                var appendContent = GenerateForContentTemplate(appendTemplateId.Value, emailTemplateTargetType, emailTemplateTargetId, localisationService, explicitTokenDictionary: explicitTokenDictionary);
                response.Content += "\n" + appendContent.Content;
            }

            return response;
        }

        public GenerateContentResponse PopulateTemplateContent(string emailTemplateTargetType, IEnumerable<Guid> targetRecordIds, LocalisationService localisationService, Dictionary<string, string> explicitTokenDictionary, string templateContent, string templateSubject)
        {
            string contentResult = null;
            string subjectResult = templateSubject;

            if (targetRecordIds != null && targetRecordIds.Any())
            {

                var targetTokens = new List<string>();
                var staticTokens = new Dictionary<string, List<string>>();
                var ifTokens = new List<string>();
                var fetchTokens = new List<string>();

                explicitTokenDictionary = explicitTokenDictionary ?? new Dictionary<string, string>();

                templateContent = ReplaceExplicitTokens(localisationService, explicitTokenDictionary, templateContent);

                //extract all tokens in the content for processing and/or including in queries for target records
                ParseOutTokens(templateContent, targetTokens, staticTokens, ifTokens, fetchTokens);
                ParseOutTokens(subjectResult, targetTokens, staticTokens, ifTokens, fetchTokens);

                var targetObjects = GetTargetObjects(emailTemplateTargetType, targetRecordIds, targetTokens);

                foreach (var templateId in targetRecordIds)
                {
                    var targetRecord = targetObjects.Any(e => e.Id == templateId)
                        ? targetObjects.First(e => e.Id == templateId)
                        : null;
                    if (targetRecord != null)
                    {
                        var thisItemsTemplateContent = templateContent;
                        thisItemsTemplateContent = ProcessFetchXmlTokens(emailTemplateTargetType, localisationService, explicitTokenDictionary, targetTokens, fetchTokens, targetRecord, thisItemsTemplateContent);

                        thisItemsTemplateContent = ProcessIfTokens(explicitTokenDictionary, thisItemsTemplateContent, ifTokens, targetRecord);

                        //replace all the target tokens
                        foreach (var token in targetTokens)
                        {
                            var sourceType = emailTemplateTargetType;
                            string displayString = GetDisplayString(targetRecord, token, isHtml: true);
                            thisItemsTemplateContent = thisItemsTemplateContent.Replace("[" + token + "]", displayString);
                            if (subjectResult != null)
                            {
                                subjectResult = subjectResult.Replace("[" + token + "]", displayString);
                            }
                        }

                        thisItemsTemplateContent = ReplaceStaticTokens(localisationService, staticTokens, thisItemsTemplateContent);

                        string removeThisFunkyChar = "\xFEFF";
                        if (thisItemsTemplateContent != null)
                            thisItemsTemplateContent = thisItemsTemplateContent.Replace(removeThisFunkyChar, "");

                        contentResult += thisItemsTemplateContent;
                    }
                }
            }

            var response = new GenerateContentResponse();
            response.Content = contentResult;
            response.Subject = subjectResult;
            return response;
        }

        private string ReplaceStaticTokens(LocalisationService localisationService, Dictionary<string, List<string>> staticTokens, string thisItemsTemplateContent)
        {
            foreach (var staticTargetTokens in staticTokens)
            {
                var staticType = staticTargetTokens.Key;
                var staticFields = staticTargetTokens.Value;

                //query to get all the fields for replacing tokens
                var staticQuery = BuildSourceQuery(staticType, staticFields);
                var staticTarget = XrmService.RetrieveFirst(staticQuery);

                //replace all the tokens
                foreach (var staticField in staticFields)
                {
                    string staticFunc = null;
                    thisItemsTemplateContent = thisItemsTemplateContent.Replace("[static|" + string.Format("{0}.{1}", staticType, staticField) + "]", XrmService.GetFieldAsDisplayString(staticType, staticField, staticTarget.GetField(staticField), localisationService, isHtml: true, func: staticFunc));
                }
            }

            return thisItemsTemplateContent;
        }

        private string ProcessFetchXmlTokens(string emailTemplateTargetType, LocalisationService localisationService, Dictionary<string, string> explicitTokenDictionary, List<string> targetTokens, List<string> fetchTokens, Entity targetRecord, string thisItemsTemplateContent)
        {
            //process fetches first as content has tokens only relevant for the subquery
            foreach (var fetchToken in fetchTokens)
            {
                var indexOfToken = thisItemsTemplateContent.IndexOf(fetchToken);
                var remainingContent = thisItemsTemplateContent.Substring(indexOfToken);
                var startFetch = remainingContent.IndexOf("<fetch");
                var lengthToEndFetch = remainingContent.IndexOf("/fetch>");
                var subIndexOfEndForFetch = remainingContent.IndexOf(endforfetchIdentifier);
                if (subIndexOfEndForFetch < 1)
                {
                    throw new Exception($"Missing '{endforfetchIdentifier}' after '{forfetchIdentifier}");
                }

                var fetchXml = remainingContent.Substring(startFetch, lengthToEndFetch + 7 - startFetch);
                var repeatingContent = remainingContent.Substring(lengthToEndFetch + 8, subIndexOfEndForFetch - lengthToEndFetch - 8 - 1);
                //replace tokens in fetchXml
                foreach (var token in targetTokens)
                {
                    var sourceType = emailTemplateTargetType;

                    var fetchValue = GetQueryValueForToken(targetRecord, token);
                    if (fetchValue == null)
                    {
                        throw new Exception($"Token '{token}' within fetchXml resolved to a null value. Fetch tokens are required to be populated through target record");
                    }
                    fetchXml = fetchXml.Replace("[" + token + "]", fetchValue?.ToString());
                }

                var fetchResults = XrmService.Fetch(fetchXml);
                var contentForFetchResult = fetchResults.Any()
                    ? PopulateTemplateContent(fetchResults.First().LogicalName, fetchResults.Select(e => e.Id), localisationService, explicitTokenDictionary, repeatingContent, null).Content
                    : null;
                thisItemsTemplateContent = thisItemsTemplateContent.Substring(0, indexOfToken - 1)
                    + contentForFetchResult
                    + remainingContent.Substring(subIndexOfEndForFetch + endforfetchIdentifier.Length + 1);
            }

            return thisItemsTemplateContent;
        }

        private List<Entity> GetTargetObjects(string emailTemplateTargetType, IEnumerable<Guid> targetRecordIds, List<string> targetTokens)
        {
            var targetObjects = new List<Entity>();

            var throwAwayList = targetRecordIds.ToList();
            while (throwAwayList.Any())
            {
                var theseIds = throwAwayList.Take(50).ToArray();
                throwAwayList.RemoveRange(0, theseIds.Count());
                var query = XrmService.BuildSourceQuery(emailTemplateTargetType, targetTokens);
                query.Criteria.AddCondition(new ConditionExpression(XrmService.GetPrimaryKey(emailTemplateTargetType), ConditionOperator.In, theseIds.Cast<object>().ToArray()));
                targetObjects.AddRange(XrmService.RetrieveAll(query));
            }

            return targetObjects;
        }

        private static string ReplaceExplicitTokens(LocalisationService localisationService, Dictionary<string, string> explicitTokenDictionary, string contentDescription)
        {
            AddToken(explicitTokenDictionary, "TODAY", localisationService.ToDateDisplayString(localisationService.TargetToday));
            AddToken(explicitTokenDictionary, "1DAY", localisationService.ToDateDisplayString(localisationService.TargetToday.AddDays(1)));
            AddToken(explicitTokenDictionary, "2DAYS", localisationService.ToDateDisplayString(localisationService.TargetToday.AddDays(2)));
            AddToken(explicitTokenDictionary, "7DAYS", localisationService.ToDateDisplayString(localisationService.TargetToday.AddDays(7)));
            if (explicitTokenDictionary != null)
            {
                foreach (var item in explicitTokenDictionary)
                {
                    contentDescription = contentDescription.Replace("[" + item.Key + "]", item.Value);
                }
            }

            return contentDescription;
        }

        private static void ParseOutTokens(string content, List<string> targetTokens, Dictionary<string, List<string>> staticTokens, List<string> ifTokens, List<string> fetchTokens)
        {
            //parse out all tokens inside [] chars to replace in the subject
            if (content != null)
            {
                var j = 0;
                while (j < content.Length)
                {
                    if (content[j] == '[')
                    {
                        var startIndex = j;
                        while (j < content.Length)
                        {
                            if (content[j] == ']')
                            {
                                var endIndex = j;
                                var token = content.Substring(startIndex + 1, endIndex - startIndex - 1);

                                if (token.ToLower().StartsWith(ifIdentifier) || token.ToLower().StartsWith(endifIdentifier))
                                {
                                    ifTokens.Add(token);
                                }
                                else if (token.ToLower().StartsWith(staticIdentifier))
                                {
                                    token = token.Substring(staticIdentifier.Length);
                                    var split = token.Split('.');
                                    if (split.Count() != 2)
                                        throw new Exception(string.Format("The static token {0} is not formatted as expected. It should be of the form type.field", token));
                                    var staticType = split.First();
                                    var staticField = split.ElementAt(1);
                                    if (!staticTokens.ContainsKey(staticType))
                                        staticTokens.Add(staticType, new List<string>());
                                    staticTokens[staticType].Add(staticField);
                                }
                                else if(token.ToLower().StartsWith(endforfetchIdentifier))
                                {
                                    //nothing required at end fetch?
                                }
                                else if (token.ToLower().StartsWith(forfetchIdentifier) || token.ToLower().StartsWith(endforfetchIdentifier))
                                {
                                    var remainingPart = content.Substring(startIndex);
                                    var endForfetchIndex = remainingPart.IndexOf(endforfetchIdentifier);
                                    if (endForfetchIndex == -1)
                                    {
                                        throw new Exception($"Missing '{endforfetchIdentifier}' token after '{forfetchIdentifier}'");
                                    }
                                    var subEndIndex = endForfetchIndex + endforfetchIdentifier.Length + 1;
                                    j = startIndex + subEndIndex;
                                    token = content.Substring(startIndex, subEndIndex);
                                    //for a fetch taken lets put the entire fetch section as the token
                                    var endFetchIndex = remainingPart.IndexOf("</fetch>");
                                    if(endFetchIndex == -1)
                                    {
                                        throw new Exception($"Missing '</fetch>' token after '{forfetchIdentifier}'");
                                    }
                                    var fetchPart = remainingPart.Substring(forfetchIdentifier.Length + 1, endFetchIndex + "</fetch>".Length - (forfetchIdentifier.Length + 1));
                                    ParseOutTokens(fetchPart, targetTokens, staticTokens, ifTokens, fetchTokens);
                                    fetchTokens.Add(token);
                                    //skip the content within fetch for now as this will have tokens for a sub query
                                }
                                else
                                {
                                    targetTokens.Add(token);
                                }
                                break;
                            }
                            j++;
                        }
                    }
                    else
                        j++;
                }
            }
        }

        /// <summary>
        /// Returns list of key values giving the types and field name parsed for the given string of field joins
        /// key = type, value = field
        /// </summary>
        /// <param name="xrmService"></param>
        /// <param name="fieldPath"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, string>> GetTypeFieldPath(string fieldPath, string sourceType)
        {

            var list = new List<KeyValuePair<string, string>>();
            var splitOutFunction = fieldPath.Split(':');
            if (splitOutFunction.Count() > 1)
                fieldPath = splitOutFunction.ElementAt(1);
            var split = fieldPath.Split('.');
            var currentType = sourceType;
            list.Add(new KeyValuePair<string, string>(currentType, split.ElementAt(0).Split('|').First()));
            var i = 1;
            if (split.Length > 1)
            {
                foreach (var item in split.Skip(1).Take(split.Length - 1))
                {
                    var fieldName = item.Split('|').First();
                    if (split.ElementAt(i - 1).Contains("|"))
                    {
                        var targetType = split.ElementAt(i - 1).Split('|').Last();
                        list.Add(new KeyValuePair<string, string>(targetType, fieldName));
                        currentType = targetType;
                    }
                    else
                    {
                        var fieldPart = list.ElementAt(i - 1).Value;
                        var targetTypes = XrmService.GetLookupTargets(fieldPart, currentType);
                        if (targetTypes.Contains(","))
                        {
                            throw new Exception($"Error parsing query. Field {fieldPart} on type {currentType} has multiple target types but none is specifed in the template. Suffix the field name with '|targettype' to correct the error.");
                        }
                        var targetTypePart = targetTypes.Split(',').First();
                        list.Add(new KeyValuePair<string, string>(targetTypePart, fieldName));
                        currentType = targetTypePart;
                    }
                    i++;
                }
            }
            return list;
        }

        /// <summary>
        /// Returns a query containing all the fields, and required joins for all the given fields
        /// field examples are "did_contactid.firstname" or "customerid|contact.lastname"
        public QueryExpression BuildSourceQuery(string sourceType, IEnumerable<string> fields)
        {
            var query = XrmService.BuildQuery(sourceType, new string[0], null, null);
            foreach (var field in fields)
            {
                XrmService.AddRequiredQueryJoins(query, field);
            }
            return query;
        }

        public void AddRequiredQueryJoins(QueryExpression query, string source)
        {
            var typeFieldPaths = XrmService.GetTypeFieldPath(source, query.EntityName);
            var splitOutFunction = source.Split(':');
            if (splitOutFunction.Count() > 1)
                source = splitOutFunction.ElementAt(1);
            var splitTokens = source.Split('.');
            if (typeFieldPaths.Count() == 1)
                query.ColumnSet.AddColumn(typeFieldPaths.First().Value);
            else
            {
                LinkEntity thisLink = null;

                for (var i = 0; i < typeFieldPaths.Count() - 1; i++)
                {
                    var lookupField = typeFieldPaths.ElementAt(i).Value;
                    var path = string.Join(".", splitTokens.Take(i + 1)).Replace("|", "_");
                    var targetType = typeFieldPaths.ElementAt(i + 1).Key;
                    if (i == 0)
                    {
                        var matchingLinks = query.LinkEntities.Where(le => le.EntityAlias == path);

                        if (matchingLinks.Any())
                            thisLink = matchingLinks.First();
                        else
                        {
                            thisLink = query.AddLink(targetType, lookupField, XrmService.GetPrimaryKey(targetType), JoinOperator.LeftOuter);
                            thisLink.EntityAlias = path;
                            thisLink.Columns = XrmService.CreateColumnSet(new string[0]);
                        }
                    }
                    else
                    {
                        var matchingLinks = thisLink.LinkEntities.Where(le => le.EntityAlias == path);
                        if (matchingLinks.Any())
                            thisLink = matchingLinks.First();
                        else
                        {
                            thisLink = thisLink.AddLink(targetType, lookupField, XrmService.GetPrimaryKey(targetType), JoinOperator.LeftOuter);
                            thisLink.EntityAlias = path;
                            thisLink.Columns = XrmService.CreateColumnSet(new string[0]);

                        }

                    }
                }
                thisLink.Columns.AddColumn(typeFieldPaths.ElementAt(typeFieldPaths.Count() - 1).Value);
            }
        }


        public string GetDisplayLabel(Entity targetObject, string token)
        {
            var fieldPaths = XrmService.GetTypeFieldPath(token, targetObject.LogicalName);
            var thisFieldType = fieldPaths.Last().Key;
            var thisFieldName = fieldPaths.Last().Value;
            var displayString = XrmService.GetFieldLabel(thisFieldName, thisFieldType);
            return displayString;
        }
        public object GetQueryValueForToken(Entity targetObject, string token, bool isHtml = false)
        {
            var fieldPaths = XrmService.GetTypeFieldPath(token, targetObject.LogicalName);
            var thisFieldType = fieldPaths.Last().Key;
            var thisFieldName = fieldPaths.Last().Value;
            string func = null;
            var getFieldString = token.Replace("|", "_");
            var splitFunc = getFieldString.Split(':');
            if (splitFunc.Count() > 1)
            {
                func = splitFunc.First();
                getFieldString = splitFunc.ElementAt(1);
            }
            return ConvertToQueryValue(targetObject.GetField(getFieldString));
        }

        public string GetDisplayString(Entity targetObject, string token, bool isHtml = false)
        {
            var fieldPaths = XrmService.GetTypeFieldPath(token, targetObject.LogicalName);
            var thisFieldType = fieldPaths.Last().Key;
            var thisFieldName = fieldPaths.Last().Value;
            string func = null;
            var getFieldString = token.Replace("|", "_");
            var splitFunc = getFieldString.Split(':');
            if (splitFunc.Count() > 1)
            {
                func = splitFunc.First();
                getFieldString = splitFunc.ElementAt(1);
            }
            var tokenFieldValue = targetObject.GetField(getFieldString);
            var displayString = XrmService.GetFieldAsDisplayString(thisFieldType, thisFieldName, tokenFieldValue, LocalisationService, isHtml: isHtml, func: func);
            return displayString;
        }

        public class GenerateContentResponse
        {
            public string Subject { get; set; }
            public string Content { get; set; }
        }



        private string ProcessIfTokens(Dictionary<string, string> explicitTokenDictionary, string activityDescription, List<string> ifTokens, Entity targetObject)
        {
            //process all the ifs (clear where not)
            while (ifTokens.Any())
            {
                var endIfTokenStackCount = 0;
                var removeAll = false;
                var token = ifTokens.First();
                if (token.ToLower() != endifIdentifier)
                {
                    var ifTokenIndex = activityDescription.IndexOf(token);
                    var tokenIndexOfSeparator = token.IndexOf("|");
                    if (tokenIndexOfSeparator > -1)
                    {
                        var tokenName = token.Substring(tokenIndexOfSeparator + 1).Replace("|", "_");

                        //LoaderOptimization until get to this ones endif
                        var endIfTokenStack = 1;
                        var remainingTokens = ifTokens.Skip(1).ToList();

                        var innerTokens = new List<string>();
                        while (remainingTokens.Any())
                        {
                            var nextToken = remainingTokens.First();
                            if (nextToken.ToLower() == endifIdentifier)
                            {
                                endIfTokenStack--;
                                endIfTokenStackCount++;
                            }
                            else
                            {
                                endIfTokenStack++;
                            }
                            remainingTokens.RemoveAt(0);
                            if (endIfTokenStack == 0)
                            {
                                break;
                            }
                            innerTokens.Add(nextToken);
                        }
                        //okay so starting at the current index need to find the end if
                        //and remove the content or the tokens
                        var currentStack = endIfTokenStackCount;
                        var currentIndex = activityDescription.IndexOf(token);
                        while (currentStack > 0)
                        {
                            var endIfIndex = activityDescription.IndexOf(endifIdentifier, currentIndex + 1, StringComparison.OrdinalIgnoreCase);
                            if (endIfIndex > -1)
                            {
                                currentIndex = endIfIndex;
                                currentStack--;
                            }
                            else
                                break;
                        }
                        //if we have inner tokens then recursivelly replace the inner part for thos tokens
                        if (innerTokens.Any())
                        {
                            var startRemove = ifTokenIndex - 1;
                            var endRemove = currentIndex - 1;
                            var innerDescription = activityDescription.Substring(startRemove + token.Length + 2, endRemove - startRemove - token.Length - 2);
                            var innerDescriptionprocessed = ProcessIfTokens(explicitTokenDictionary, innerDescription, innerTokens, targetObject);
                            currentIndex = currentIndex - (innerDescription.Length - innerDescriptionprocessed.Length);
                            activityDescription = activityDescription.Substring(0, startRemove + token.Length + 2)
                                + innerDescriptionprocessed
                                + activityDescription.Substring(endRemove);
                        }

                        if (explicitTokenDictionary.ContainsKey(tokenName))
                        {
                            removeAll = explicitTokenDictionary[tokenName] is string s
                                && (s == null || s.ToLower() == "false");
                        }
                        else
                        {
                            var fieldValue = targetObject.GetField(tokenName);
                            removeAll = fieldValue == null;
                        }
                        if (removeAll)
                        {
                            var startRemove = ifTokenIndex - 1;
                            var endRemove = currentIndex + endifIdentifier.Length + 1;
                            activityDescription = activityDescription.Substring(0, startRemove) + activityDescription.Substring(endRemove);
                        }
                        else
                        {
                            var startRemove = ifTokenIndex - 1;
                            var endRemove = currentIndex - 1;
                            activityDescription = activityDescription.Substring(0, startRemove)
                                + activityDescription.Substring(startRemove + token.Length + 2, endRemove - startRemove - token.Length - 2)
                                + activityDescription.Substring(endRemove + endifIdentifier.Length + 2);
                        }
                    }
                }
                ifTokens.RemoveRange(0, endIfTokenStackCount > 0 ? endIfTokenStackCount * 2 : 1);
            }

            return activityDescription;
        }

        private static void AddToken(Dictionary<string, string> explicitTokenDictionary, string key, string value)
        {
            if (!explicitTokenDictionary.ContainsKey(key))
                explicitTokenDictionary.Add(key, value);
        }

        public object ConvertToQueryValue(object value)
        {
            if (value is EntityReference er)
                value = er.Id;
            else if (value is OptionSetValue osv)
                value = osv.Value;
            else if (value is Money m)
                value = m.Value;
            return value;
        }
    }
}
