# Minify

Minify is a modern WPF desktop application for Windows that provides a minimal, responsive interface to control Spotify playback. It leverages the [Spotify Web API](https://developer.spotify.com/documentation/web-api/) to display current track information, album art, and playback controls, all in a compact and visually appealing window.

## Features

- **Spotify Authentication:** Secure OAuth2 login with token refresh support.
- **Playback Controls:** Play, pause, skip, previous, and seek within tracks.
- **Track Information:** Displays current song title, artist, album art, duration, and progress.
- **Volume Control:** Adjust Spotify playback volume directly from the app.
- **Live Updates:** UI updates in real-time as playback state changes.
- **Minimal API Usage:** Efficiently polls Spotify only when necessary to reduce API calls.
- **Modern UI:** Built with [MahApps.Metro](https://github.com/MahApps/MahApps.Metro) and [ControlzEx](https://github.com/ControlzEx/ControlzEx) for a sleek, dark-themed interface.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A [Spotify Developer Application](https://developer.spotify.com/dashboard/applications) (for Client ID and Secret)
- Spotify Premium account (required for playback control via API)

### Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/minify.git
   cd minify
   ```


2. **Configure Spotify Credentials:**
- Open `MainWindow.xaml.cs`.
- Replace `<CLIENT ID>` and `<CLIENT SECRET>` with your Spotify app credentials.
- Ensure your Spotify app's Redirect URI is set to `http://localhost:5000/callback` in the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/applications).

3. **Restore NuGet Packages:**
- Open the solution in Visual Studio 2022.
- Restore NuGet packages if not done automatically.

4. **Build and Run:**
- Press `F5` or select __Debug > Start Debugging__.

### Usage

- On first launch, you will be prompted to log in to your Spotify account.
- After authentication, the app will display the current track and playback controls.
- Use the play/pause, next, previous, seek, and volume controls as desired.

## Project Structure

- `Minify/MainWindow.xaml` & `MainWindow.xaml.cs`: Main WPF window and logic.
- `Minify/SpotifyService.cs`: Encapsulates all Spotify API interactions.
- `Minify/TokenStorage.cs`: Handles secure storage and retrieval of OAuth tokens.

## Dependencies

- [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET)
- [MahApps.Metro](https://github.com/MahApps/MahApps.Metro)
- [ControlzEx](https://github.com/ControlzEx/ControlzEx)
- [EmbedIO](https://github.com/unosquare/embedio) (for local OAuth callback)

## Security

- OAuth tokens are stored locally for session persistence.
- **Never commit your Client Secret or tokens to a public repository.**

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Acknowledgments

- [Spotify Developer Platform](https://developer.spotify.com/)
- [MahApps.Metro](https://github.com/MahApps/MahApps.Metro)
- [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET)

---

*Minify is not affiliated with or endorsed by Spotify AB.*
