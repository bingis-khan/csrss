// Data structures associated with RSS.

using System.Xml;
using System.Xml.Linq;


class Feed
{
	public readonly string? Error;
	public readonly Channel? Channel;

	private Feed(string? error, XDocument? content)
	{
		this.Error = error;
		this.Channel = null;
	}

	public Feed(string content)
	{
		try
		{
			var xml = XDocument.Parse(content);

			var xmlChannel = xml.Get("rss").Get("channel");
			var xmlItems = xmlChannel?.Elements("item") ?? Enumerable.Empty<XElement>();

			var items = xmlItems.Select(item => 
			{
				var pubDate = item.Get("pubDate")?.Value;
				return new Item(
					Title: item.Get("title")?.Value?.Trim(),
					Description: item.Get("description")?.Value?.Trim(),
					Author: item.Get("author")?.Value?.Trim(),
					Link: item.Get("link")?.Value?.Trim(),
					PubDate: pubDate != null ? DateTime.Parse(pubDate) : null,
					Guid: item.Get("guid")?.Value?.Trim()
				);
			}).Where(item => !item.Empty()).ToArray();

			var channel = new Channel(
				Title: xmlChannel.Get("title")?.Value?.Trim(),
				Link: xmlChannel.Get("link")?.Value?.Trim(),
				Description: xmlChannel.Get("description")?.Value?.Trim(),
				Items: items
			);
			
			this.Error = null;
			this.Channel = channel;
		}
		catch (XmlException e)
		{
			this.Error = e.ToString();
			this.Channel = null;
		}
	}

	// when it fails.
	public static Feed Failed(string error) => new(error, null);

	public override string ToString()
	{
		var description = Channel.Description != null ? "(" + Channel.Description + ")" : "";
		var items = string.Concat(Channel.Items.Select(item => $"\t{item.Title ?? item.Description ?? "<uh oh>"}\n"));
		return $"Feed {Channel.Title ?? "<no chan title>"} {description}:\n{items}";
	}

}

static class FindElement
{
	public static XElement? Get(this XContainer? elem, string name) => elem?.Elements(name)?.SingleOrDefault();
}

record Channel(string? Title, string? Link, string? Description, Item[] Items);
record Item(string? Title, string? Description, string? Author, string? Link, DateTime? PubDate, string? Guid)
{
	public bool Empty() => Title == null && Description == null && Author == null && Link == null && PubDate == null && Guid == null;
}
