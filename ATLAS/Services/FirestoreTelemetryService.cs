using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace ATLAS.Services
{
    public class FirestoreTelemetryService
    {
        private FirestoreDb? _firestoreDb;
        private static FirestoreTelemetryService? _instance;
        private static readonly object _lock = new object();

        // Thread-safe Lazy Initialization Singleton Pattern
        public static FirestoreTelemetryService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new FirestoreTelemetryService();
                    }
                    return _instance;
                }
            }
        }

        private FirestoreTelemetryService()
        {
            // Constructor is intentionally left lightweight to prevent early startup crashes
        }

        private void EnsureInitialized()
        {
            if (_firestoreDb == null)
            {
                if (string.IsNullOrWhiteSpace(FirebaseConfig.ProjectId))
                {
                    throw new InvalidOperationException("Firebase Project ID is missing or unconfigured in FirebaseConfig.cs");
                }

                // Initialize context on-demand only when a scanning action triggers
                _firestoreDb = FirestoreDb.Create(FirebaseConfig.ProjectId);
            }
        }

        public async Task SaveScanTelemetryAsync(string analysisType, float resultScore, bool isScam)
        {
            if (!AuthService.IsLoggedIn || string.IsNullOrEmpty(AuthService.CurrentUserId))
                return;

            try
            {
                // Defer database connection validation safely until this line
                EnsureInitialized();

                CollectionReference collection = _firestoreDb!.Collection("analyses");

                var documentData = new Dictionary<string, object>
                {
                    { "user_id", AuthService.CurrentUserId },
                    { "analysis_type", analysisType },
                    { "result_score", resultScore },
                    { "is_scam", isScam },
                    { "created_at", DateTime.UtcNow }
                };

                await collection.AddAsync(documentData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firestore Database Sync Blocked]: {ex.Message}");
            }
        }

        public async Task<(int TotalScans, int ScamCount, int SafeCount)> GetUserStatsAsync()
        {
            if (!AuthService.IsLoggedIn || string.IsNullOrEmpty(AuthService.CurrentUserId))
                return (0, 0, 0);

            try
            {
                // Query all analysis records corresponding to the logged-in User ID
                Query query = _firestoreDb.Collection("analyses").WhereEqualTo("user_id", AuthService.CurrentUserId);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                int total = snapshot.Count;
                int scams = 0;
                int safe = 0;

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc.TryGetValue("is_scam", out bool isScam))
                    {
                        if (isScam) scams++;
                        else safe++;
                    }
                }
                return (total, scams, safe);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving Firestore statistics: {ex.Message}");
                return (0, 0, 0);
            }
        }
    }
}