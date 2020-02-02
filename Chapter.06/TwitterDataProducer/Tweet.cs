using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Tweetinvi.Models;
using Tweetinvi.Models.DTO;
using Tweetinvi.Models.Entities;
using Tweetinvi.Parameters;

namespace TwitterDataProducer
{
    public class Tweet : ITweet
    {
        private readonly JToken _dto;

        public Tweet(JToken json)
        {
            _dto = json;
        }

        public int PublishedTweetLength => throw new NotImplementedException();

        public DateTime CreatedAt => throw new NotImplementedException();

        public string Text
        {
            get => _dto["text"].ToString();
            set => throw new NotImplementedException();
        }

        public string Prefix => throw new NotImplementedException();

        public string Suffix => throw new NotImplementedException();

        public string FullText
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public int[] DisplayTextRange => throw new NotImplementedException();

        public IExtendedTweet ExtendedTweet
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public ICoordinates Coordinates
        {
            get
            {
                var token = _dto["coordinates"];
                if (token != null)
                    return new Coordinates(double.Parse(token["Latitude"].ToString()),
                        double.Parse(token["Longitude"].ToString()));
                return null;
            }

            set => throw new NotImplementedException();
        }

        public string Source
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool Truncated => throw new NotImplementedException();
        public int? ReplyCount { get; set; }

        public long? InReplyToStatusId
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public string InReplyToStatusIdStr
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public long? InReplyToUserId
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public string InReplyToUserIdStr
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public string InReplyToScreenName
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public IUser CreatedBy => new User(_dto["user"]);

        public ITweetIdentifier CurrentUserRetweetIdentifier => throw new NotImplementedException();

        public int[] ContributorsIds => throw new NotImplementedException();

        public IEnumerable<long> Contributors => throw new NotImplementedException();

        public int RetweetCount => throw new NotImplementedException();

        public ITweetEntities Entities => throw new NotImplementedException();

        public bool Favorited => throw new NotImplementedException();

        public int FavoriteCount => throw new NotImplementedException();

        public bool Retweeted => throw new NotImplementedException();

        public bool PossiblySensitive => throw new NotImplementedException();

        public Language? Language => throw new NotImplementedException();

        public IPlace Place => throw new NotImplementedException();

        public Dictionary<string, object> Scopes => throw new NotImplementedException();

        public string FilterLevel => throw new NotImplementedException();

        public bool WithheldCopyright => throw new NotImplementedException();

        public IEnumerable<string> WithheldInCountries => throw new NotImplementedException();

        public string WithheldScope => throw new NotImplementedException();

        public ITweetDTO TweetDTO { get; set; }

        public DateTime TweetLocalCreationDate => throw new NotImplementedException();

        public List<IHashtagEntity> Hashtags => throw new NotImplementedException();

        public List<IUrlEntity> Urls => throw new NotImplementedException();

        public List<IMediaEntity> Media => throw new NotImplementedException();

        public List<IUserMentionEntity> UserMentions => throw new NotImplementedException();

        public List<ITweet> Retweets
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool IsRetweet => throw new NotImplementedException();

        public ITweet RetweetedTweet => throw new NotImplementedException();
        public int? QuoteCount { get; set; }

        public long? QuotedStatusId => throw new NotImplementedException();

        public string QuotedStatusIdStr => throw new NotImplementedException();

        public ITweet QuotedTweet => throw new NotImplementedException();

        public bool IsTweetPublished => throw new NotImplementedException();

        public bool IsTweetDestroyed => throw new NotImplementedException();

        public string Url => throw new NotImplementedException();

        public long Id => throw new NotImplementedException();

        public string IdStr => throw new NotImplementedException();

        public int[] SafeDisplayTextRange => throw new NotImplementedException();

        public bool Destroy()
        {
            throw new NotImplementedException();
        }

        public Task<bool> DestroyAsync()
        {
            throw new NotImplementedException();
        }

        public bool Equals(ITweet other)
        {
            throw new NotImplementedException();
        }

        public void Favorite()
        {
            throw new NotImplementedException();
        }

        public Task FavoriteAsync()
        {
            throw new NotImplementedException();
        }

        public IOEmbedTweet GenerateOEmbedTweet()
        {
            throw new NotImplementedException();
        }

        public Task<IOEmbedTweet> GenerateOEmbedTweetAsync()
        {
            throw new NotImplementedException();
        }

        public List<ITweet> GetRetweets()
        {
            throw new NotImplementedException();
        }

        public Task<List<ITweet>> GetRetweetsAsync()
        {
            throw new NotImplementedException();
        }

        public ITweet PublishRetweet()
        {
            throw new NotImplementedException();
        }

        public Task<ITweet> PublishRetweetAsync()
        {
            throw new NotImplementedException();
        }

        public void UnFavorite()
        {
            throw new NotImplementedException();
        }

        public Task UnFavoriteAsync()
        {
            throw new NotImplementedException();
        }

        public bool UnRetweet()
        {
            throw new NotImplementedException();
        }

        public int CalculateLength(bool willBePublishedWithMedia)
        {
            throw new NotImplementedException();
        }
    }

    public class User : IUser
    {
        private readonly JToken _dto;

        public User(JToken json)
        {
            _dto = json;
        }

        public Language Language => throw new NotImplementedException();

        public IUserDTO UserDTO
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public IUserIdentifier UserIdentifier => throw new NotImplementedException();

        public string Name => _dto["name"].ToString();

        public string Description => throw new NotImplementedException();

        public ITweetDTO Status => throw new NotImplementedException();

        public DateTime CreatedAt => throw new NotImplementedException();

        public string Location => throw new NotImplementedException();

        public bool GeoEnabled => throw new NotImplementedException();

        public string Url => throw new NotImplementedException();

        public int StatusesCount => throw new NotImplementedException();

        public int FollowersCount => throw new NotImplementedException();

        public int FriendsCount => throw new NotImplementedException();

        public bool Following => throw new NotImplementedException();

        public bool Protected => throw new NotImplementedException();

        public bool Verified => throw new NotImplementedException();

        public IUserEntities Entities => throw new NotImplementedException();

        public bool Notifications => throw new NotImplementedException();

        public string ProfileImageUrl => throw new NotImplementedException();

        public string ProfileImageUrlFullSize => throw new NotImplementedException();

        public string ProfileImageUrl400x400 => throw new NotImplementedException();

        public string ProfileImageUrlHttps => throw new NotImplementedException();

        public bool FollowRequestSent => throw new NotImplementedException();

        public bool DefaultProfile => throw new NotImplementedException();

        public bool DefaultProfileImage => throw new NotImplementedException();

        public int FavouritesCount => throw new NotImplementedException();

        public int ListedCount => throw new NotImplementedException();

        public string ProfileSidebarFillColor => throw new NotImplementedException();

        public string ProfileSidebarBorderColor => throw new NotImplementedException();

        public bool ProfileBackgroundTile => throw new NotImplementedException();

        public string ProfileBackgroundColor => throw new NotImplementedException();

        public string ProfileBackgroundImageUrl => throw new NotImplementedException();

        public string ProfileBackgroundImageUrlHttps => throw new NotImplementedException();

        public string ProfileBannerURL => throw new NotImplementedException();

        public string ProfileTextColor => throw new NotImplementedException();

        public string ProfileLinkColor => throw new NotImplementedException();

        public bool ProfileUseBackgroundImage => throw new NotImplementedException();

        public bool IsTranslator => throw new NotImplementedException();

        public bool ContributorsEnabled => throw new NotImplementedException();

        public int? UtcOffset => throw new NotImplementedException();

        public string TimeZone => throw new NotImplementedException();

        public IEnumerable<string> WithheldInCountries => throw new NotImplementedException();

        public string WithheldScope => throw new NotImplementedException();

        public List<long> FriendIds
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public List<IUser> Friends
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public List<long> FollowerIds
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public List<IUser> Followers
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public List<IUser> Contributors
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public List<IUser> Contributees
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public List<ITweet> Timeline
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public List<ITweet> Retweets
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public List<ITweet> FriendsRetweets
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public List<ITweet> TweetsRetweetedByFollowers
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public long Id
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public string IdStr
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public string ScreenName
        {
            get => _dto["screen_name"].ToString();
            set => throw new NotImplementedException();
        }

        public Task<bool> BlockAsync()
        {
            throw new NotImplementedException();
        }

        public bool BlockUser()
        {
            throw new NotImplementedException();
        }

        public bool Equals(IUser other)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IUser> GetContributees(bool createContributeeList = false)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IUser>> GetContributeesAsync(bool createContributeeList = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IUser> GetContributors(bool createContributorList = false)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IUser>> GetContributorsAsync(bool createContributorList = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITweet> GetFavorites(int maximumNumberOfTweets = 40)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITweet> GetFavorites(IGetUserFavoritesParameters parameters)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ITweet>> GetFavoritesAsync(int maximumTweets = 40)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ITweet>> GetFavoritesAsync(IGetUserFavoritesParameters parameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<long> GetFollowerIds(int maxFriendsToRetrieve = 5000)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<long>> GetFollowerIdsAsync(int maxFriendsToRetrieve = 5000)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IUser> GetFollowers(int maxFriendsToRetrieve = 250)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IUser>> GetFollowersAsync(int maxFriendsToRetrieve = 250)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<long> GetFriendIds(int maxFriendsToRetrieve = 5000)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<long>> GetFriendIdsAsync(int maxFriendsToRetrieve = 5000)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IUser> GetFriends(int maxFriendsToRetrieve = 250)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IUser>> GetFriendsAsync(int maxFriendsToRetrieve = 250)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITwitterList> GetOwnedLists(int maximumNumberOfListsToRetrieve)
        {
            throw new NotImplementedException();
        }

        public Stream GetProfileImageStream(ImageSize imageSize = ImageSize.normal)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetProfileImageStreamAsync(ImageSize imageSize = ImageSize.normal)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITwitterList> GetSubscribedLists(int maximumNumberOfListsToRetrieve = 1000)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITweet> GetUserTimeline(int maximumNumberOfTweets = 40)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITweet> GetUserTimeline(IUserTimelineParameters timelineParameters)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ITweet>> GetUserTimelineAsync(int maximumTweet = 40)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ITweet>> GetUserTimelineAsync(IUserTimelineParameters timelineParameters)
        {
            throw new NotImplementedException();
        }

        public bool ReportUserForSpam()
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnBlockAsync()
        {
            throw new NotImplementedException();
        }

        public bool UnBlockUser()
        {
            throw new NotImplementedException();
        }

        public IRelationshipDetails GetRelationshipWith(IUserIdentifier user)
        {
            throw new NotImplementedException();
        }

        public Task<IRelationshipDetails> GetRelationshipWithAsync(IUserIdentifier user)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{ScreenName} ({Name})";
        }

        public IRelationshipDetails GetRelationshipWith(IUser user)
        {
            throw new NotImplementedException();
        }

        public Task<IRelationshipDetails> GetRelationshipWithAsync(IUser user)
        {
            throw new NotImplementedException();
        }
    }
}