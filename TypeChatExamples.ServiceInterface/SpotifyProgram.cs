using ServiceStack;
using SpotifyAPI.Web;
using TypeChatExamples.ServiceModel;

namespace TypeChatExamples.ServiceInterface;

public class SpotifyProgram : SpotifyProgramBase
{
    private SpotifyClient spotifyClient;
    
    public override void Init()
    {
        spotifyClient = new SpotifyClient(Config["SpotifyToken"]);
    }
    
    public async Task play(TrackList trackList, int? startIndex = null, int? count = null)
    {
        var tracks = trackList.Skip(startIndex ?? 0).Take(count ?? trackList.Count);
        var uris = tracks.Map(x => x.Uri);
        await spotifyClient.Player.ResumePlayback(new PlayerResumePlaybackRequest {
            Uris = uris.ToList(),
        });
    }

    public async Task<TrackList> searchTracks(string query, SearchRequest.Types filterType = SearchRequest.Types.Track)
    {
        var result = await spotifyClient.Search.Item(new SearchRequest(filterType, query));

        List<Track> tracks = new TrackList();
        var tracksList = new TrackList();
        switch (filterType)
        {
            case SearchRequest.Types.Album:
                var tRes = await spotifyClient.Albums.GetSeveral(new AlbumsRequest(result.Albums.Items?.Select(x => x.Id).ToList() ?? new List<string>()));
                tracks.AddRange(tRes.Albums.Select(x => {
                        return x.Tracks.Items?.Select(y => new Track { Name = y.Name, Uri = y.Uri, Album = x.Name });
                    })
                    .SelectMany(x => x ?? new List<Track>())
                    .Select(x => new Track { Name = x.Name, Uri = x.Uri, Album = x.Name}));
                break;
            case SearchRequest.Types.Artist:
                var aRes = await spotifyClient.Artists.GetSeveral(new ArtistsRequest(result.Artists.Items?.Select(x => x.Id).ToList() ?? new List<string>()));
                var aTracks = await spotifyClient.Artists.GetTopTracks(aRes.Artists.First().Id, new ArtistsTopTracksRequest("US"));
                tracks.AddRange(aTracks.Tracks.Select(x => new Track { Name = x.Name, Uri = x.Uri, Album = x.Album.Name}));
                break;
            case SearchRequest.Types.Track:
                tracks = result.Tracks.Items?.Select(x => new Track { Name = x.Name, Uri = x.Uri }).ToList() ?? new TrackList();
                break;
        }
        
        tracksList.AddRange(tracks);

        return tracksList;
    }

    public async Task<List<Track>> getQueue()
    {
        // Logic to fetch and display upcoming tracks in the queue
        var queueResponse = await spotifyClient.Player.GetQueue();
        var tracks = queueResponse.Queue.Where(x => x.Type == ItemType.Track).Select(x => x as SpotifyAPI.Web.FullTrack).ToList();
        return tracks.Select(x => new Track { Name = x.Name, Uri = x.Uri, Album = x.Album.Name}).ToList();
    }

    public async Task<CurrentlyPlayingContext> status()
    {
        var playback = await spotifyClient.Player.GetCurrentPlayback();
        // Logic to display the current playback status
        return playback;
    }

    public async Task pause()
    {
        await spotifyClient.Player.PausePlayback();
    }

    public async Task next()
    {
        await spotifyClient.Player.SkipNext();
    }

    public async Task previous()
    {
        await spotifyClient.Player.SkipPrevious();
    }

    public async Task shuffleOn()
    {
        await spotifyClient.Player.SetShuffle(new PlayerShuffleRequest(true));
    }

    public async Task shuffleOff()
    {
        await spotifyClient.Player.SetShuffle(new PlayerShuffleRequest(false));
    }

    public async Task resume()
    {
        await spotifyClient.Player.ResumePlayback();
    }

    public async Task setVolume(int newVolumeLevel)
    {
        await spotifyClient.Player.SetVolume(new PlayerVolumeRequest(newVolumeLevel));
    }

    public async Task<TrackList> getAlbum(string name)
    {
        var result = await spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Album, name));
        var trackResult = new TrackList();
        if (result.Albums.Items != null)
        {
            var album = result.Albums.Items.First();
            var albumTracks = await spotifyClient.Albums.GetTracks(album.Id);
            trackResult.AddRange(albumTracks.Items.Select(x => new Track { Name = x.Name, Uri = x.Uri, Album = album.Name }));
        }

        return trackResult;
    }
    
    public void unknownAction(string text)
    {
        // Logic for unknown actions
    }

    public void nonMusicQuestion(string text)
    {
        // Logic for non-music questions
    }

}

public class Track
{
    public string Name { get; set; }
    public string Uri { get; set; }
    public string Album { get; set; }
}

public class TrackList : List<Track> { }

public class Playlist : TrackList
{
    public string Id { get; set; }
}

public enum SpotifyFilterType
{
    Album,
    Artist,
    Track
}

public class SpotifyProgramBase
{
    public SpotifyRunDetails RunDetails { get; set; }
    public Dictionary<string, string> Config { get; set; }

    public SpotifyProgramBase()
    {
        Config = new Dictionary<string, string>();
        RunDetails = new SpotifyRunDetails();
    }

    public SpotifyProgramBase(Dictionary<string, string> config)
    {
        Config = config;
        RunDetails = new SpotifyRunDetails();
    }

    public virtual void Init()
    {
    }
}
