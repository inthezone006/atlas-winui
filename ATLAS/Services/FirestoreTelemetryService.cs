using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace ATLAS.Services
{
    public class FirestoreTelemetryService
    {
        private readonly FirestoreDb _firestoreDb;

        public FirestoreTelemetryService()
        {
            // Initializes connection to your Firestore database space
            _firestoreDb = FirestoreDb.Create(FirebaseConfig.ProjectId);
        }

        public async Task SaveScanTelemetryAsync(string analysisType, float resultScore, bool isScam)
        {
            // If the user isn't logged in, skip cloud logging (or keep it local only)
            if (!AuthService.IsLoggedIn || string.IsNullOrEmpty(AuthService.CurrentUserId))
                return;

            try
            {
                // References the "analyses" collection we provisioned in Firebase Console
                CollectionReference collection = _firestoreDb.Collection("analyses");

                var documentData = new Dictionary<string, object>
                {
                    { "user_id", AuthService.CurrentUserId },
                    { "analysis_type", analysisType }, // e.g. "text", "image", "audio"
                    { "result_score", resultScore },
                    { "is_scam", isScam },
                    { "created_at", DateTime.UtcNow } // Stored in ISO UTC
                };

                await collection.AddAsync(documentData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firestore DB Sync Failure: {ex.Message}");
            }
        }
    }
}