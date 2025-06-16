using ControlzEx.Theming;
using MahApps.Metro.Controls; //for MahApps.Metro controls
using SpotifyAPI.Web; //main Spotify API library
using SpotifyAPI.Web.Auth; //handle spotify authentication
using System.Windows; //needed for Window
using System.Windows.Input;
using System.Windows.Media.Imaging;

/*
 
Change my song "stream" to a timer, If the song is paused, the timer should stop, and when the song is resumed, the timer should continue from where it left off.
If the timer reaches the duration of a song, get the playback state and refresh the track info.

This is to stop the program for making requests to the Spotify API every second.

Should speed up the refresh wait because it isnt waiting for the API to respond every second.

on load, load the current playback state, title, artist, album art, duration, progress, and volume.

 */

namespace Minify
{
    public partial class MainWindow : MetroWindow
    {
        private SpotifyClient? spotify; // this is our spotify client for use after login.
        private SpotifyService? spotifyService; // Add an instance of SpotifyService

        // GUI Variables
        private bool isPlaying = false; // Track if the music is playing
        private string TrackName = string.Empty; // Track name
        private string ArtistName = string.Empty; // Artist name
        private string AlbumArtUrl = string.Empty; // URL for album art
        private int TrackDurationMs = 0; // Track duration in milliseconds
        private int TrackProgressMs = 0; // Track progress in milliseconds
        private int Volume = 0; // Volume level
        private DateTime PlaybackStartTime; // Start time of playback

        // Timers
        private readonly System.Windows.Threading.DispatcherTimer progressTimer = new System.Windows.Threading.DispatcherTimer(); // Timer for updating progress bar 
        private readonly System.Timers.Timer tokenRefreshTimer = new System.Timers.Timer(); // Timer for refreshing access token

        // Flags to track user interaction with the UI
        private bool isUserSeeking = false; // Track if the user is seeking in the track
        private bool isVolumeSeeking = false; // Track if the user is seeking in the volume slider

        public MainWindow()
        {
            InitializeComponent(); // Initialize the window and its components
            ThemeManager.Current.ChangeTheme(this, "Dark.Lime"); // Set the theme to Dark Blue
            Loaded += MainWindow_Loaded; // Attach the Loaded event handler to the window
            progressTimer.Interval = TimeSpan.FromSeconds(1); // Set timer interval to 1 second
            progressTimer.Tick += progressTimer_Tick; // Attach the update method to the timer tick event
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // This method is called when the window is loaded
            // We can initialize any components or services here if needed
            var savedToken = TokenStorage.LoadToken(); // Load saved token from file
            if (savedToken != null)
            {
                await RefreshAccessToken(savedToken.RefreshToken); // If token exists, refresh it
            }
            else
            {
                // If no token is saved, start authentication process
                Authenticate();
            }
            // start refresh timer to update the access token periodically
            tokenRefreshTimer.Interval = 55 * 60 * 1000; // Set timer to refresh token every 55 minutes
            tokenRefreshTimer.Elapsed += async (s, e) =>
            {
                try
                {
                    await RefreshAccessToken(savedToken.RefreshToken); // Refresh the access token
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Token Error", ex.Message, MessageBoxButton.OK, MessageBoxImage.Warning); // Show error if token refresh fails
                    });
                }
            };
            var state = spotifyService.GetPlaybackStateAsync();
            tokenRefreshTimer.Start(); // Start the timer
        }

        protected override void OnClosed(EventArgs e)
        {
            tokenRefreshTimer.Stop(); // Stop the token refresh timer when the window is closed
            tokenRefreshTimer.Dispose(); // Dispose of the timer resources
            base.OnClosed(e); // Call the base class OnClosed method
        }

        // Handles Spotify login and stores access token
        private async void Authenticate()
        {
            // Create default config for Spotify client
            var config = SpotifyClientConfig.CreateDefault();

            // Set up login request - Opens browser for user to log in
            var request = new LoginRequest(
                new Uri("http://localhost:5000/callback"), // Where we redirect after login
                "<CLIENT ID>",        // Your Client ID
                LoginRequest.ResponseType.Code             // Ask for a code in response
            )
            {
                Scope = new[]
                {
                        // Permissions we are requesting
                        Scopes.UserReadPlaybackState,     // Read what is currently playing 
                        Scopes.UserModifyPlaybackState,   // Control playback (play/pause, skip, etc.)
                        Scopes.UserReadCurrentlyPlaying,  // See the current track
                        Scopes.PlaylistReadPrivate,       // Access user's private playlists
                        Scopes.Streaming                  // Enable stream controls
                }
            };

            // Create url and open in browser for login
            var uri = request.ToUri();
            BrowserUtil.Open(uri);

            // Make web server to listen for the login redirect
            var http = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await http.Start();

            http.AuthorizationCodeReceived += async (sender, response) =>
            {
                await http.Stop(); // Stop server

                // Exchange the code we got for an access token
                var tokenResponse = await new OAuthClient().RequestToken(
                    new AuthorizationCodeTokenRequest(
                        "<CLIENT ID>",       // Client ID
                        "<CLEINT SECRET>",       // Client Secret
                        response.Code,                            // Code we got
                        new Uri("http://localhost:5000/callback") // Must match the redirect URI EXACTLY
                    )
                );

                // Store the access token for use later
                spotify = new SpotifyClient(config.WithToken(tokenResponse.AccessToken));

                // Initialize SpotifyService with the authenticated SpotifyClient
                spotifyService = new SpotifyService(spotify);

                var tokenInfo = new TokenInfo
                {
                    AccessToken = tokenResponse.AccessToken, // Store access token
                    RefreshToken = tokenResponse.RefreshToken // Store refresh token
                };

                TokenStorage.SaveToken(tokenInfo); // Save token to file
                await LoadTrackInfo(); // Load track info after authentication

                // Prompt login success
                MessageBox.Show("Authenticated!");

                // await spotifyService.DebugTrackInfo(); // Print track info to console for debugging
                // System.Diagnostics.Debug.WriteLine("Authenticated!"); // Print to console for debugging
            };
        }

        private async Task RefreshAccessToken(string refreshToken)
        {
            try
            {
                var tokenResponse = await new OAuthClient().RequestToken(
                    new AuthorizationCodeRefreshRequest(
                        "<CLIENT ID>",       // Client ID
                        "<CLENT SECRET>",       // Client Secret
                        refreshToken     // Load refresh token from storage
                    ));
                var tokenInfo = new TokenInfo
                {
                    AccessToken = tokenResponse.AccessToken, // Update access token
                    RefreshToken = tokenResponse.RefreshToken ?? refreshToken// Update refresh token
                };
                TokenStorage.SaveToken(tokenInfo); // Save updated token to file
                var config = SpotifyClientConfig.CreateDefault();
                spotify = new SpotifyClient(config.WithToken(tokenResponse.AccessToken)); // Create new SpotifyClient with updated token
                spotifyService = new SpotifyService(spotify); // Reinitialize SpotifyService with the new client

                await LoadTrackInfo(); // Refresh track info after token refresh
            }
            catch (Exception ex)
            {
                MessageBox.Show("Token Error", ex.Message, MessageBoxButton.OK, MessageBoxImage.Warning);
                Authenticate(); // Re-authenticate if there's an error with the token
            }
        }


        // Util Functions
        private async Task RefreshPlaybackFromSpotify()
        {
            var track = await spotifyService.GetCurrentTrackAsync();
            var coverArtUrl = await spotifyService.GetCoverArtUrlAsync();
            var status = await spotifyService.GetPlaybackStateAsync();

            if (track == null)
            {
                MessageBox.Show("No track is currently playing.", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Exit if no track is playing
            }

            isPlaying = status.isPlaying; // Update playing state

            TrackName = track.Name; // Update track name
            ArtistName = track.Artists.FirstOrDefault()?.Name ?? "Unknown Artist"; // Update artist name
            AlbumArtUrl = coverArtUrl; // Update album art URL

            TrackDurationMs = status.durationMs; // Update track duration
            TrackProgressMs = status.progressMs; // Update track progress
            Volume = status.currVolume; // Update volume level

        }


        // Set the progress bar value when the user drags it
        private async void progressTimer_Tick(object? sender, EventArgs e)
        {
            if (spotifyService == null) return; // Check if spotifyService is initialized
            //System.Diagnostics.Debug.WriteLine(isUserSeeking);
            if (!isUserSeeking && !isVolumeSeeking)
            {
                await LoadTrackInfo(); // Update track info every second

            }
        }

        private void TrackProgress_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (spotifyService == null || TrackProgress == null) return;

            //var pos = e.GetPosition(TrackProgress); // Get mouse position
            //var ratio = pos.X / TrackProgress.ActualWidth; // Calculate ratio of mouse position to progress bar width
            //ratio = Math.Max(0, Math.Min(1, ratio)); // Clamp ratio between 0 and 1

            //var status = await spotifyService.GetPlaybackStateAsync(); // Get current playback state

            //int durationMs = status.durationMs; // Get track duration in milliseconds

            //int newPositionMs = (int)(durationMs * ratio); // Calculate new position in milliseconds
            isUserSeeking = true; // Set seeking state to true
            //await spotifyService.SeekAsync(newPositionMs); // Seek to new position

        }

        private async void TrackProgress_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (spotifyService == null) return; // Check if spotifyService is initialized
            try
            {
                var status = await spotifyService.GetPlaybackStateAsync(); // Get current playback state
                int durationMs = status.durationMs; // Get track duration in milliseconds
                double ratio = TrackProgress.Value / TrackProgress.Maximum; // Calculate ratio of current position to track duration
                int newPositionMs = (int)(durationMs * ratio); // Calculate new position in milliseconds

                await spotifyService.SeekAsync(newPositionMs); // Seek to new position
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning); // Show error if no devices are available
            }
            finally
            {
                isUserSeeking = false; // Reset seeking state
                await LoadTrackInfo(); // Refresh track info after seeking
            }
        }

        private void TrackProgress_MouseLeave(object sender, MouseEventArgs e)
        {
            isUserSeeking = false;
        }

        // Update the track info on the UI
        private async Task LoadTrackInfo()
        {
            try
            {
                var track = await spotifyService.GetCurrentTrackAsync();
                var coverArtUrl = await spotifyService.GetCoverArtUrlAsync();
                var status = await spotifyService.GetPlaybackStateAsync();
                isPlaying = status.isPlaying;

                if (status.isPlaying)
                {
                    progressTimer.Start();
                }
                else
                {
                    progressTimer.Stop();
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    if (track != null)
                    {
                        TrackTitle.Text = track.Name;
                        TrackArtist.Text = string.Join(", ", track.Artists.Select(a => a.Name));
                        AlbumArt.Source = new BitmapImage(new Uri(coverArtUrl));

                        // Only update progress if not seeking
                        if (!isUserSeeking)
                        {
                            TrackProgress.Maximum = status.durationMs;
                            TrackProgress.Value = status.progressMs;
                        }

                        CurrentTime.Text = spotifyService.FormatTime(status.progressMs);
                        TotalTime.Text = spotifyService.FormatTime(status.durationMs);

                        // Only update volume if not seeking
                        if (!isVolumeSeeking)
                        {
                            VolumeSlider.Value = status.currVolume;
                            //System.Diagnostics.Debug.WriteLine(status.currVolume);
                        }
                    }

                    PlayPauseButton.Content = isPlaying ? "⏸" : "▶";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading track info: " + ex.Message);
                _ = RefreshAccessToken(TokenStorage.LoadToken()?.RefreshToken ?? string.Empty); // Attempt to refresh token if there's an error
            }
        }

        private void Volume_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (spotifyService == null || VolumeSlider == null) return; // Check if spotifyService and VolumeSlider are initialized
            isVolumeSeeking = true; // Set seeking state to true
        }

        private async void Volume_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (spotifyService == null) return; // Check if spotifyService is initialized
            try
            {
                var volume = e.GetPosition(VolumeSlider).X / VolumeSlider.ActualWidth; // Get mouse position and calculate volume ratio
                volume = Math.Max(0, Math.Min(1, volume)); // Clamp volume ratio between 0 and 1
                await spotifyService.SetVolumeAsync((int)(volume * 100)); // Set volume based on mouse position
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning); // Show error if no devices are available
            }
            finally
            {
                isVolumeSeeking = false; // Reset seeking state
                await LoadTrackInfo();
            }
        }

        private void Volume_MouseLeave(object sender, MouseEventArgs e)
        {
            isVolumeSeeking = false;
        }

        //private async void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    if (spotifyService == null) return;

        //    int volumePercent = (int)e.NewValue;
        //    try
        //    {
        //        await spotifyService.SetVolumeAsync(volumePercent);
        //    }
        //    catch
        //    {
        //        MessageBox.Show("No active device found. Please start Spotify on a device.", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning); // Show error if no devices are available
        //    }
        //}

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
                try
                {
                    await spotifyService.PauseAsync(); // Pause playback
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning); // Show error if no devices are available
                }
            else
                try
                {
                    await spotifyService.PlayAsync(); // Resume playback
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning); // Show error if no devices are available
                }
            await LoadTrackInfo(); // Refresh track info after play/pause
        }

        private async void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await spotifyService.SkipToPreviousAsync(); // Skip to previous track
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning); // Show error if no devices are available
            }
            await LoadTrackInfo(); // Refresh track info after skipping
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await spotifyService.SkipToNextAsync(); // Skip to next track
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning); // Show error if no devices are available
            }
            await LoadTrackInfo(); // Refresh track info after skipping
        }
    }
}