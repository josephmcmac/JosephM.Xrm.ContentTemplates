using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.Xrm.Sdk;
using Schema;
using System;

namespace JosephM.Xrm.ContentTemplates.Plugins.Localisation
{
    public class UserLocalisationSettings : ILocalisationSettings
    {
        public UserLocalisationSettings(XrmService xrmService, Guid userId)
        {
            XrmService = xrmService;
            CurrentUserId = userId;
        }

        private Entity _userSettings;
        private Entity UserSettings
        {
            get
            {
                if (_userSettings == null)
                {
                    _userSettings = XrmService.GetFirst(Entities.usersettings, Fields.usersettings_.systemuserid, CurrentUserId, new[] { Fields.usersettings_.timezonecode, Fields.usersettings_.dateformatstring });
                    if (_userSettings == null)
                        throw new NullReferenceException($"Error getting {XrmService.GetEntityDisplayName(Entities.usersettings)} for user with ID {CurrentUserId}");
                }
                return _userSettings;
            }
        }

        public string DateFormat
        {
            get
            {
                var dateFormat = UserSettings.GetStringField(Fields.usersettings_.dateformatstring);
                if (string.IsNullOrWhiteSpace(dateFormat))
                {
                    throw new NullReferenceException("Error dateformatstring is empty in the usersettings record");
                }
                return dateFormat;
            }
        }

        private int UserTimeZoneCode
        {
            get
            {
                var userTimeZoneCode = UserSettings.GetField(Fields.usersettings_.timezonecode);
                if (userTimeZoneCode == null)
                {
                    throw new NullReferenceException("Error timezonecode is empty in the usersettings record");
                }
                return (int)userTimeZoneCode;
            }
        }

        private Entity _timeZone;
        private Entity TimeZone
        {
            get
            {
                if (_timeZone == null)
                {
                    _timeZone = XrmService.GetFirst(Entities.timezonedefinition, Fields.timezonedefinition_.timezonecode, UserTimeZoneCode, new[] { Fields.timezonedefinition_.standardname });
                }
                return _timeZone;
            }
        }

        public XrmService XrmService { get; }
        public Guid CurrentUserId { get; }

        public string TargetTimeZoneId => TimeZone.GetStringField(Fields.timezonedefinition_.standardname);
    }
}