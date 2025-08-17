using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation; // Add this for animations
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
            // The timer now ticks less frequently
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200); // How often a new emoji fades in
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void HomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            timer?.Stop(); // Clean up the timer
        }

        private void BackgroundCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Center the main ATLAS title
            Canvas.SetLeft(AtlasTitle, (e.NewSize.Width - AtlasTitle.ActualWidth) / 2);
            Canvas.SetTop(AtlasTitle, (e.NewSize.Height - AtlasTitle.ActualHeight) / 2);
            InitializeSpottedGrid();
        }

        private void InitializeSpottedGrid()
        {
            foreach (var emoji in emojiList)
            {
                BackgroundCanvas.Children.Remove(emoji);
            }
            emojiList.Clear();

            // Create a grid of initially invisible emojis
            for (int i = 0; i < 50; i++)
            {
                var emoji = new TextBlock
                {
                    Text = "🗺️",
                    FontSize = 48,
                    Opacity = 0.0 // Start completely invisible
                };

                Canvas.SetLeft(emoji, random.NextDouble() * BackgroundCanvas.ActualWidth);
                Canvas.SetTop(emoji, random.NextDouble() * BackgroundCanvas.ActualHeight);

                emojiList.Add(emoji);
                BackgroundCanvas.Children.Add(emoji);
            }
        }

        // This method now handles the fade animation
        private void Timer_Tick(object? sender, object e)
        {
            if (emojiList.Count == 0) return;

            // 1. Pick a random, currently invisible emoji
            TextBlock? targetEmoji = null;
            for (int i = 0; i < 5; i++) // Try a few times to find an idle emoji
            {
                var potentialTarget = emojiList[random.Next(emojiList.Count)];
                if (potentialTarget.Opacity < 0.01)
                {
                    targetEmoji = potentialTarget;
                    break;
                }
            }
            if (targetEmoji == null) return; // Skip if no idle emoji was found

            // 2. Create the fade-in and fade-out animation
            var storyboard = new Storyboard();
            var fadeAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 0.15, // Fade to a subtle opacity
                Duration = new Duration(TimeSpan.FromSeconds(2)), // Fade-in duration
                AutoReverse = true // Automatically fade back out
            };

            // 3. Target the animation to the emoji's Opacity property
            Storyboard.SetTarget(fadeAnimation, targetEmoji);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
            storyboard.Children.Add(fadeAnimation);

            // 4. Run the animation
            storyboard.Begin();
        }
    }
}