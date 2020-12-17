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
    /// A service class for performing logic
    /// </summary>
    public class JosephMContentTemplatesService
    {
        private XrmService XrmService { get; set; }
        public LocalisationService LocalisationService { get; set; }
        private JosephMContentTemplatesSettings JosephMContentTemplatesSettings { get; set; }

        public JosephMContentTemplatesService(XrmService xrmService, LocalisationService localisationService, JosephMContentTemplatesSettings settings)
        {
            XrmService = xrmService;
            LocalisationService = localisationService;
            JosephMContentTemplatesSettings = settings;
        }

        public GenerateEmailContentResponse GetContent(Guid contentTemplateId, string emailTemplateTargetType, Guid emailTemplateTargetId, LocalisationService localisationService, Dictionary<string, string> explicitTokenDictionary = null)
        {
            var response = new GenerateEmailContentResponse();

            var targetTokens = new List<string>();
            var staticTokens = new Dictionary<string, List<string>>();
            var ifTokens = new List<string>();
            var staticIdentifier = "static|";
            var ifIdentifier = "if|";
            var endifIdentifier = "endif";

            var resource = XrmService.Retrieve(Entities.jmcg_contenttemplate, contentTemplateId);
            var contentDescription = resource.GetStringField(Fields.jmcg_contenttemplate_.jmcg_content);
            var contentSubject = resource.GetStringField(Fields.jmcg_contenttemplate_.jmcg_subject);
            var appendTemplateId = resource.GetLookupGuid(Fields.jmcg_contenttemplate_.jmcg_appendtemplate);

            //replace all explicit tokens e.g. [TODAY] => ~DateTime.Today.ToString("dd/MM/yyyy")
            explicitTokenDictionary = explicitTokenDictionary ?? new Dictionary<string, string>();
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

            //parse out all tokens inside [] chars to replace in the content
            var i = 0;
            while (i < contentDescription.Length)
            {
                if (contentDescription[i] == '[')
                {
                    var startIndex = i;
                    while (i < contentDescription.Length)
                    {
                        if (contentDescription[i] == ']')
                        {
                            var endIndex = i;
                            var token = contentDescription.Substring(startIndex + 1, endIndex - startIndex - 1);

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
                            else
                            {
                                targetTokens.Add(token);
                            }
                            break;
                        }
                        i++;
                    }
                }
                else
                    i++;
            }

            //parse out all tokens inside [] chars to replace in the subject
            if (contentSubject != null)
            {
                var j = 0;
                while (j < contentSubject.Length)
                {
                    if (contentSubject[j] == '[')
                    {
                        var startIndex = j;
                        while (j < contentSubject.Length)
                        {
                            if (contentSubject[j] == ']')
                            {
                                var endIndex = j;
                                var token = contentSubject.Substring(startIndex + 1, endIndex - startIndex - 1);

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

            //query to get all the fields for replacing tokens
            var query = XrmService.BuildSourceQuery(emailTemplateTargetType, targetTokens);
            query.Criteria.AddCondition(new ConditionExpression(XrmService.GetPrimaryKey(emailTemplateTargetType), ConditionOperator.Equal, emailTemplateTargetId));
            var targetObject = XrmService.RetrieveFirst(query);

            contentDescription = ProcessIfTokens(explicitTokenDictionary, contentDescription, ifTokens, endifIdentifier, targetObject);

            //replace all the tokens
            foreach (var token in targetTokens)
            {
                var sourceType = emailTemplateTargetType;
                string displayString = GetDisplayString(targetObject, token, isHtml: true);
                contentDescription = contentDescription.Replace("[" + token + "]", displayString);
                if (contentSubject != null)
                {
                    contentSubject = contentSubject.Replace("[" + token + "]", displayString);
                }
            }

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
                    contentDescription = contentDescription.Replace("[static|" + string.Format("{0}.{1}", staticType, staticField) + "]", XrmService.GetFieldAsDisplayString(staticType, staticField, staticTarget.GetField(staticField), localisationService, isHtml: true, func: staticFunc));
                }
            }

            string removeThisFunkyChar = "\xFEFF";
            if (contentDescription != null)
                contentDescription = contentDescription.Replace(removeThisFunkyChar, "");

            if (appendTemplateId.HasValue)
            {
                var appendContent = GetContent(appendTemplateId.Value, emailTemplateTargetType, emailTemplateTargetId, localisationService, explicitTokenDictionary: explicitTokenDictionary);
                contentDescription = contentDescription + "\n" + appendContent.Content;
            }

            response.Content = contentDescription;
            response.Subject = contentSubject;
            return response;
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
            var displayString = XrmService.GetFieldAsDisplayString(thisFieldType, thisFieldName, targetObject.GetField(getFieldString), LocalisationService, isHtml: isHtml, func: func);
            return displayString;
        }


        public class GenerateEmailContentResponse
        {
            public string Subject { get; set; }
            public string Content { get; set; }
        }



        private string ProcessIfTokens(Dictionary<string, string> explicitTokenDictionary, string activityDescription, List<string> ifTokens, string endifIdentifier, Entity targetObject)
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
                            var innerDescriptionprocessed = ProcessIfTokens(explicitTokenDictionary, innerDescription, innerTokens, endifIdentifier, targetObject);
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
    }
}
