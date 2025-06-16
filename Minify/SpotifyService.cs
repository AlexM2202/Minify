using SpotifyAPI.Web;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Minify
{
    public class SpotifyService
    {
        private readonly SpotifyClient spotify; // This is our connection to the API

        // Constructor
        public SpotifyService(SpotifyClient client)
        {
            spotify = client;
        }

        private async Task<bool> EnsureActiveDeviceAsync()
        {
            var devices = await spotify.Player.GetAvailableDevices();
            return devices.Devices.Any(d => d.IsActive); // Check if any device is active
        }

        private async Task ExecuteWithActiveDevice(Func<Task> action)
        {
            if (!await EnsureActiveDeviceAsync())
            {
                throw new Exception("No active device found. Please start playback on a device first.");
            }
            await action(); // Execute the action if an active device is found
        }

        public async Task DebugTrackInfo()
        {
            var track = await GetCurrentTrackAsync();
            if (track == null)
            {
                System.Diagnostics.Debug.WriteLine("No track is currently playing.");
                return;
            }
            System.Diagnostics.Debug.WriteLine($"Track: {track.Name}"); // Print track name
            System.Diagnostics.Debug.WriteLine($"Artist: {string.Join(", ", track.Artists.Select(a => a.Name))}"); // Print artist name(s)
            System.Diagnostics.Debug.WriteLine($"Album Art URL: {track.Album.Images.FirstOrDefault()?.Url}"); // Print album art URL
        }

        /*
         Basic Controls for Spotify playback.
         Play, Pause, Skip, Back
         */

        // PlayAsync - Tell Spotify to start playing or resume
        public async Task PlayAsync()
        {
            await ExecuteWithActiveDevice(async () =>
            {
                await spotify.Player.ResumePlayback(); // Ask Spotify to play
            });
        }

        // PauseAsync - Tell Spotify to pause playback
        public async Task PauseAsync()
        {
            await ExecuteWithActiveDevice(async () =>
            {
                await spotify.Player.PausePlayback(); // Ask Spotify to pause
            });
        }

        // SkipToNextAsync - Tell Spotify to skip to the next track
        public async Task SkipToNextAsync()
        {
            await ExecuteWithActiveDevice(async () =>
            {
                await spotify.Player.SkipNext(); // Ask Spotify to skip to the next track
            });
        }

        // SkipToPreviousAsync - Tell Spotify to skip to the previous track
        public async Task SkipToPreviousAsync()
        {
            await ExecuteWithActiveDevice(async () =>
            {
                await spotify.Player.SkipPrevious(); // Ask Spotify to skip to the previous track
            });
        }

        // Seek to the specified position in milliseconds
        public async Task SeekAsync(int positionMs)
        {
            await spotify.Player.SeekTo(new PlayerSeekToRequest(positionMs)); 
        }

        public async Task SetVolumeAsync(int volumePercent)
        {
            await ExecuteWithActiveDevice(async () =>
            {
                await spotify.Player.SetVolume(new PlayerVolumeRequest(volumePercent));
            });
        }

        /*
         Complex Functions
         Get Current Track, Get Cover Art, Get Time
         */

        // GetCurrentTrackAsync - Get the currently playing track
        public async Task<FullTrack?> GetCurrentTrackAsync()
        {
            // Get the currently playing track from Spotify

            var currentlyPlaying = await spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest()); // Get the currently playing track
            return currentlyPlaying?.Item as FullTrack; // Return the track if it exists, otherwise null
            
            
        }

        // GetCoverArtUrlAsync - Get the cover art URL for the current track        
        public async Task<string?> GetCoverArtUrlAsync()
        {
            var track = await GetCurrentTrackAsync(); // Get the current track
            return track?.Album?.Images?.FirstOrDefault()?.Url; // Get the cover art URL if it exists
        }

        // GetPlaybackStateAsync - Get the playback state (are we playing? where are we? how long is it?)
        public async Task<(bool isPlaying, int progressMs, int durationMs, int currVolume)> GetPlaybackStateAsync()
        {
            var playback = await spotify.Player.GetCurrentPlayback(); // Get the playback status

            return (
                isPlaying: playback?.IsPlaying ?? false, // Check if it's playing
                progressMs: playback?.ProgressMs ?? 0, // Get the current progress in milliseconds
                durationMs: (playback?.Item as FullTrack)?.DurationMs ?? 0, // Get the total duration in milliseconds
                currVolume: playback?.Device?.VolumePercent ?? 0 // Get the current volume percentage
            );
        }

        // FormatTime - Make time readable for the user
        public string FormatTime(int milliseconds)
        {
            var ts = TimeSpan.FromMilliseconds(milliseconds); // Convert milliseconds to TimeSpan
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}"; // Format as MM:SS
        }
    }
}
