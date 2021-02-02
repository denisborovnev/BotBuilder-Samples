// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class TeamsMessagingExtensionsSearchBot : TeamsActivityHandler
    {
        // NOTE: adaptive card is shown with app icon/title
        //protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query, CancellationToken cancellationToken)
        //{
        //    var text = query?.Parameters?[0]?.Value as string ?? string.Empty;

        //    var packages = await FindPackages(text);

        //    var attachments = packages.Select(package => {
        //        var previewCard = new ThumbnailCard
        //        {
        //            Title = package.Item1,
        //        };
        //        if (!string.IsNullOrEmpty(package.Item5))
        //        {
        //            previewCard.Images = new List<CardImage>() { new CardImage(package.Item5, "Icon") };
        //        }

        //        var attachment = new MessagingExtensionAttachment
        //        {
        //            ContentType = AdaptiveCard.ContentType,
        //            Content = CreateCard(package.Item1, package.Item2),

        //            Preview = previewCard.ToAttachment()
        //        };

        //        return attachment;
        //    }).ToList();

        //    return new MessagingExtensionResponse
        //    {
        //        ComposeExtension = new MessagingExtensionResult
        //        {
        //            Type = "result",
        //            AttachmentLayout = "list",
        //            Attachments = attachments
        //        }
        //    };
        //}

        // NOTE: adaptive card is shown without app icon/title
        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query, CancellationToken cancellationToken)
        {
            var text = query?.Parameters?[0]?.Value as string ?? string.Empty;

            var packages = await FindPackages(text);

            var attachments = packages.Select(package => {
                    var previewCard = new ThumbnailCard {
                        Title = package.Item1,
                        Tap = new CardAction { Type = "invoke", Value = package }
                    };
                    if (!string.IsNullOrEmpty(package.Item5))
                    {
                        previewCard.Images = new List<CardImage>() { new CardImage(package.Item5, "Icon") };
                    }

                    var attachment = new MessagingExtensionAttachment
                    {
                        ContentType = HeroCard.ContentType,
                        Content = new HeroCard { Title = package.Item1 },

                        Preview = previewCard.ToAttachment()
                    };
                
                    return attachment;
                }).ToList();

            return new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = attachments
                }
            };
        }

        protected override Task<MessagingExtensionResponse> OnTeamsMessagingExtensionSelectItemAsync(ITurnContext<IInvokeActivity> turnContext, JObject query, CancellationToken cancellationToken)
        {
            var (packageId, version, description, projectUrl, iconUrl) = query.ToObject<(string, string, string, string, string)>();

            var card = CreateCard(packageId, version);
           

            var attachment = new MessagingExtensionAttachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
                Preview = new Attachment(HeroCard.ContentType, null, new ThumbnailCard
                {
                    Title = $"{packageId}, {version}"
                })
            };

            return Task.FromResult(new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = new List<MessagingExtensionAttachment> { attachment }
                }
            });
        }

        private AdaptiveCard CreateCard(string packageId, string version)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2));

            var titleContainer = new AdaptiveContainer();
            titleContainer.Items.Add(new AdaptiveTextBlock
            {
                Text = $"{packageId}, {version}",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true
            });
            card.Body.Add(titleContainer);

            card.Actions.Add(
                new AdaptiveOpenUrlAction { Title = "Nuget Package", Url = new System.Uri($"https://www.nuget.org/packages/{packageId}") }
            );

            return card;
        }

        private async Task<IEnumerable<(string, string, string, string, string)>> FindPackages(string text)
        {
            var obj = JObject.Parse(await (new HttpClient()).GetStringAsync($"https://azuresearch-usnc.nuget.org/query?q=id:{text}&prerelease=true"));
            return obj["data"].Select(item => (item["id"].ToString(), item["version"].ToString(), item["description"].ToString(), item["projectUrl"]?.ToString(), item["iconUrl"]?.ToString()));
        }
    }
}
