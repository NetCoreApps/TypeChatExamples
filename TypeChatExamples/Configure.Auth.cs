using ServiceStack.Auth;
using ServiceStack.FluentValidation;

[assembly: HostingStartup(typeof(TypeChatExamples.ConfigureAuth))]

namespace TypeChatExamples
{
    // Add any additional metadata properties you want to store in the Users Typed Session
    public class CustomUserSession : AuthUserSession
    {
    }
    
    // Custom Validator to add custom validators to built-in /register Service requiring DisplayName and ConfirmPassword
    public class CustomRegistrationValidator : RegistrationValidator
    {
        public CustomRegistrationValidator()
        {
            RuleSet(ApplyTo.Post, () =>
            {
                RuleFor(x => x.DisplayName).NotEmpty();
                RuleFor(x => x.ConfirmPassword).NotEmpty();
            });
        }
    }

    public class ConfigureAuth : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder) => builder
            .ConfigureServices(services => {
                //services.AddSingleton<ICacheClient>(new MemoryCacheClient()); //Store User Sessions in Memory Cache (default)
            })
            .ConfigureAppHost(appHost => {
                var appSettings = appHost.AppSettings;
                appHost.Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                    new IAuthProvider[] {
                        new CredentialsAuthProvider(appSettings),
                        new SpotifyAuthProvider(appSettings)
                        {
                            Scopes = new[]
                            {
                                "user-read-private",
                                "user-read-email",
                                "app-remote-control",
                                "user-modify-playback-state",
                                "user-read-playback-state",
                                "user-read-currently-playing"
                            }
                        }  /* Create App https://developer.spotify.com/my-applications */
                    }) {
                    HtmlRedirect = "/signin"
                });

                appHost.Plugins.Add(new RegistrationFeature()); //Enable /register Service

                //override the default registration validation with your own custom implementation
                appHost.RegisterAs<CustomRegistrationValidator, IValidator<Register>>();
            });
    }
}
