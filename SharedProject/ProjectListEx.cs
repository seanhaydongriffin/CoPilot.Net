using SharedProject.Models;
using System.Collections.Generic;

namespace SharedProject
{
    static class ProjectListEx
    {
        public static Project2 GetLastUsed(this List<Project2> projects)
        {
            var path = AppConfig.Get("ManageExecutionGroupsProjectPath");

            if (path == null)

                return projects[0];

            var result = projects.Find(x => x.path == path);

            if (result == null)

                return projects[0];

            return result;
        }
    }
}
