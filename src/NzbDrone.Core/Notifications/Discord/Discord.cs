using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Notifications.Discord.Payloads;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Discord
{
    public class Discord : NotificationBase<DiscordSettings>
    {
        private readonly IDiscordProxy _proxy;

        public Discord(IDiscordProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "Discord";
        public override string Link => "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

        public override void OnGrab(GrabMessage message)
        {
            var embeds = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Author = new DiscordAuthor
                                      {
                                          Name = "Radarr",
                                          IconUrl = "https://raw.githubusercontent.com/Radarr/Radarr/aphrodite/Logo/256.png"
                                      },
                                      Thumbnail = new DiscordImage
                                      {
                                          Url = message.Movie.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Poster).Url
                                      },
                                      Image = new DiscordImage
                                      {
                                          Url = message.Movie.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Fanart).Url
                                      },
                                      Url = $"https://www.themoviedb.org/movie/{message.Movie.TmdbId}",
                                      Description = "Grabbed",
                                      Title = message.Movie.Title,
                                      Text = message.Message,
                                      Color = (int)DiscordColors.Warning,
                                      Fields = new List<DiscordField>
                                      {
                                          new DiscordField
                                          {
                                              Name = "Overview",
                                              Value = message.Movie.Overview?.Substring(0, Math.Min(message.Movie.Overview.Length, 300))
                                          },
                                          new DiscordField
                                          {
                                              Name = "Rating",
                                              Value = message.Movie.Ratings.Value.ToString()
                                          },
                                          new DiscordField
                                          {
                                              Name = "Genres",
                                              Value = message.Movie.Genres.Take(3).Join(", ")
                                          },
                                          new DiscordField
                                          {
                                              Name = "Quality",
                                              Value = message.Quality.Quality.Name,
                                              Inline = true
                                          },
                                          new DiscordField
                                          {
                                              Name = "Group",
                                              Value = message.RemoteMovie.ParsedMovieInfo.ReleaseGroup,
                                              Inline = true
                                          },
                                          new DiscordField
                                          {
                                              Name = "Size",
                                              Value = BytesToString(message.RemoteMovie.Release.Size),
                                              Inline = true
                                          }
                                      }
                                  }
                              };
            var payload = CreatePayload($"Grabbed: {message.Message}", embeds);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnDownload(DownloadMessage message)
        {
            var embeds = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Author = new DiscordAuthor
                                      {
                                          Name = "Radarr",
                                          IconUrl = "https://raw.githubusercontent.com/Radarr/Radarr/aphrodite/Logo/256.png"
                                      },
                                      Thumbnail = new DiscordImage
                                      {
                                          Url = message.Movie.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Poster).Url
                                      },
                                      Image = new DiscordImage
                                      {
                                          Url = message.Movie.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Fanart).Url
                                      },
                                      Url = $"https://www.themoviedb.org/movie/{message.Movie.TmdbId}",
                                      Description = "Imported",
                                      Title = message.Movie.Title,
                                      Text = message.Message,
                                      Color = (int)DiscordColors.Success,
                                      Fields = new List<DiscordField>
                                      {
                                          new DiscordField
                                          {
                                              Name = "Overview",
                                              Value = message.Movie.Overview?.Substring(0, Math.Min(message.Movie.Overview.Length, 300))
                                          },
                                          new DiscordField
                                          {
                                              Name = "Rating",
                                              Value = message.Movie.Ratings.Value.ToString()
                                          },
                                          new DiscordField
                                          {
                                              Name = "Genres",
                                              Value = message.Movie.Genres.Take(3).Join(", ")
                                          },
                                          new DiscordField
                                          {
                                              Name = "Quality",
                                              Value = message.MovieFile.Quality.Quality.Name,
                                              Inline = true
                                          },
                                          new DiscordField
                                          {
                                              Name = "Group",
                                              Value = message.MovieFile.ReleaseGroup,
                                              Inline = true
                                          },
                                          new DiscordField
                                          {
                                              Name = "Size",
                                              Value = BytesToString(message.MovieFile.Size),
                                              Inline = true
                                          }
                                      }
                                  }
                              };
            var payload = CreatePayload($"Imported: {message.Message}", embeds);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Author = new DiscordAuthor
                                      {
                                          Name = "Radarr",
                                          IconUrl = "https://raw.githubusercontent.com/Radarr/Radarr/aphrodite/Logo/256.png"
                                      },
                                      Title = healthCheck.Source.Name,
                                      Text = healthCheck.Message,
                                      Color = healthCheck.Type == HealthCheck.HealthCheckResult.Warning ? (int)DiscordColors.Warning : (int)DiscordColors.Danger
                                  }
                              };

            var payload = CreatePayload("Health Issue", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(TestMessage());

            return new ValidationResult(failures);
        }

        public ValidationFailure TestMessage()
        {
            try
            {
                var message = $"Test message from Radarr posted at {DateTime.Now}";
                var payload = CreatePayload(message);

                _proxy.SendPayload(payload, Settings);
            }
            catch (DiscordException ex)
            {
                return new NzbDroneValidationFailure("Unable to post", ex.Message);
            }

            return null;
        }

        private DiscordPayload CreatePayload(string message, List<Embed> embeds = null)
        {
            var avatar = Settings.Avatar;

            var payload = new DiscordPayload
            {
                Username = Settings.Username,
                Content = message,
                Embeds = embeds
            };

            if (avatar.IsNotNullOrWhiteSpace())
            {
                payload.AvatarUrl = avatar;
            }

            if (Settings.Username.IsNotNullOrWhiteSpace())
            {
                payload.Username = Settings.Username;
            }

            return payload;
        }

        private static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
            {
                return "0" + suf[0];
            }

            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}
