using Microsoft.Graph;

namespace Entra_ID_MVC_App.Models
{
    /// <summary>
    /// ViewModel for displaying groups information
    /// Contains list of groups and handling for error scenarios
    /// </summary>
    public class GroupsViewModel
    {
        /// <summary>
        /// List of groups retrieved from Microsoft Graph
        /// </summary>
        public List<Group> Groups { get; set; } = new List<Group>();

        /// <summary>
        /// Error message if group retrieval fails
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Total count of groups
        /// </summary>
        public int TotalGroups => Groups.Count;

        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }
}