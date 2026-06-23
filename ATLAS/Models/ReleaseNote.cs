using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace ATLAS.Models
{
    public class ReleaseNote
    {
        public string Version { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string FormattedDate => ReleaseDate.ToString("MMMM dd, yyyy");

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> NewFeatures { get; set; } = new List<string>();
        public List<string> BugFixes { get; set; } = new List<string>();

        public Visibility GetVisibility(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public static class ReleaseNotesStore
    {
        public static List<ReleaseNote> GetNotes()
        {
            return new List<ReleaseNote>
            {
                new ReleaseNote
                {
                    Version = "4.0.0",
                    ReleaseDate = new DateTime(2026, 6, 23, 12, 0, 0),
                    Title = "The Gemini AI Overhaul",
                    Description = "ATLAS now offloads machine learning load to the cloud using the Gemini SDK. This massively reduces the app size and provides a deeper scam analysis across major analysis methods.",
                    NewFeatures = new List<string>
                    {
                        "Implemented cloud-based text analysis via multiple Gemini models.",
                        "Upgraded image and audio analysis to pipe directly into the new multimodal analysis engine.",
                        "Unified the Threat Classification system into professional risk buckets.",
                        "Updated results box on link and file analysis to be more space efficient.",
                        "Maintained same analysis box structure across all analysis methods."
                    },
                    BugFixes = new List<string>
                    {
                        "Removed local machine learning model dependencies, shrinking the app size by over 300MB.",
                        "Added in-build Terms of Service and Privacy Policy to reduce external dependencies."
                    }
                },
                new ReleaseNote
                {
                    Version = "3.0.0",
                    ReleaseDate = new DateTime(2026, 6, 22, 12, 0, 0),
                    Title = "The All-Inclusive Model",
                    Description = "ATLAS now includes the proprietary model within the package, reducing network latency while increasing app install size.",
                    NewFeatures = new List<string>
                    {
                        "Added local analysis via included model.",
                        "Moved database away from MongoDB to further reduce dependencies on multiple services, reducing application load time."
                    },
                    BugFixes = new List<string>
                    {
                        "Removed all references to the now deprecated ATLAS online service to perform local analysis.",
                        "Fixed a critical casting crash in animation logic."
                    }
                },
                new ReleaseNote
                {
                    Version = "2.0.0",
                    ReleaseDate = new DateTime(2026, 11, 19, 12, 0, 0),
                    Title = "The Better 1.0.0",
                    Description = "ATLAS now comes with full connection to the UI found on our online service with all API keys updated to allow for a full experience.",
                    NewFeatures = new List<string>
                    {
                        "Added Google as an option for signing in and creating accounts.",
                        "Added ability for user to connect their legacy accounts to Google to allow for easier signing in."
                    },
                    BugFixes = new List<string>
                    {
                        "Updated text on all screens to reflect the true nature of this application and its dependencies."
                    }
                },
                new ReleaseNote
                {
                    Version = "1.0.0",
                    ReleaseDate = new DateTime(2025, 10, 20, 12, 00, 0),
                    Title = "The Initial Release",
                    Description = "ATLAS is now released for Windows. This application works with the online custom-tuned model created within the team using confirmed threats online. Accounts and different analysis methods are present with plans to add more.",
                    NewFeatures = new List<string>
                    {
                        "Added user account creation with MongoDB.",
                        "Added image, audio, text analysis using custom machine learning model.",
                        "Added link and file analysis using VirusTotal."
                    },
                    BugFixes = new List<string>
                    {
                        "Fixed Google sign in issues."
                    }
                }
            };
        }
    }
}