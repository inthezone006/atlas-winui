using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace ATLAS.Pages
{
    public sealed partial class HomePage : Page
    {
        private DispatcherTimer timer;
        private List<TextBlock> emojiList = new List<TextBlock>();
        private Random random = new Random();

        public HomePage()
        {
            this.InitializeComponent();
            this.Loaded += HomePage_Loaded;
            this.Unloaded += HomePage_Unloaded;
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50); // Controls animation speed
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void HomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            timer?.Stop();
        }

        private void BackgroundCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetLeft(AtlasTitle, (e.NewSize.Width - AtlasTitle.ActualWidth) / 2);
            Canvas.SetTop(AtlasTitle, (e.NewSize.Height - AtlasTitle.ActualHeight) / 2);
            InitializeBackground();
        }

        private void InitializeBackground()
        {
            foreach (var emoji in emojiList)
            {
                BackgroundCanvas.Children.Remove(emoji);
            }
            emojiList.Clear();

            if (BackgroundCanvas.ActualWidth == 0) return;

            // FIX: Define vertical "lanes" to prevent horizontal overlap
            int numLanes = (int)(BackgroundCanvas.ActualWidth / 100); // Create a lane every 100 pixels
            if (numLanes == 0) numLanes = 1;
            double laneWidth = BackgroundCanvas.ActualWidth / numLanes;

            for (int i = 0; i < 50; i++)
            {
                var emoji = new TextBlock
                {
                    Text = "🗺️",
                    FontSize = 48,
                    Opacity = 0.1
                };

                // Assign to a random lane
                int lane = random.Next(numLanes);
                double horizontalPosition = (lane * laneWidth) + (laneWidth - emoji.FontSize) / 2;

                // Position randomly vertically
                double verticalPosition = random.NextDouble() * BackgroundCanvas.ActualHeight;

                Canvas.SetLeft(emoji, horizontalPosition);
                Canvas.SetTop(emoji, verticalPosition);

                emojiList.Add(emoji);
                BackgroundCanvas.Children.Add(emoji);
            }
        }

        private void Timer_Tick(object? sender, object e)
        {
            if (BackgroundCanvas.ActualWidth == 0) return;

            // Recalculate lanes in case window was resized
            int numLanes = (int)(BackgroundCanvas.ActualWidth / 100);
            if (numLanes == 0) numLanes = 1;
            double laneWidth = BackgroundCanvas.ActualWidth / numLanes;

            foreach (var emoji in emojiList)
            {
                double top = Canvas.GetTop(emoji);
                Canvas.SetTop(emoji, top + 1);

                if (top > BackgroundCanvas.ActualHeight)
                {
                    // If an emoji goes off-screen, reset it to the top in a new random lane
                    Canvas.SetTop(emoji, -50);

                    int lane = random.Next(numLanes);
                    double horizontalPosition = (lane * laneWidth) + (laneWidth - emoji.FontSize) / 2;
                    Canvas.SetLeft(emoji, horizontalPosition);
                }
            }
        }
    }
}