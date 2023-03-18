// See https://aka.ms/new-console-template for more information
using System.Text.Json;
using System.Text;
using JsonReader;
using System.IO;
using System.Reflection.PortableExecutable;
using System;
using ReaderSample;

await Run();

static async Task Run()
{
    // MySample();
   await new AsyncReader().RunSample();
}

static void MySample()
{
    var stream = new FileStream("youtube_sample.json", FileMode.Open);

    // var buffer = new byte[8192];
    //var buffer = new byte[4096];
    var buffer = new byte[100];
    // Fill the buffer.
    // For this snippet, we're assuming the stream is open and has data.
    // If it might be closed or empty, check if the return value is 0.
    stream.Read(buffer);

    // We set isFinalBlock to false since we expect more data in a subsequent read from the stream.
    var reader = new Utf8JsonReader(buffer, isFinalBlock: false, state: default);
    Console.WriteLine($"Initial String in buffer is: {Encoding.UTF8.GetString(buffer)}");



    bool EnsureBytesForRead(ref Utf8JsonReader reader)
    {
        while (!reader.Read() && stream.Position < stream.Length)
        {
            // Not enough of the JSON is in the buffer to complete a read.
            GetMoreBytesFromStream(stream, buffer, ref reader);
        }
        return true;
    }

    var ytItems = new List<YouTubeItem>();

    YouTubeItemId ProcessId(ref Utf8JsonReader reader)
    {
        var id = new YouTubeItemId();
        while (EnsureBytesForRead(ref reader) && reader.TokenType != JsonTokenType.EndObject)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("kind")))
                    {
                        EnsureBytesForRead(ref reader);
                        id.Kind = reader.GetString();
                        break;
                    }
                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("channelId")))
                    {
                        EnsureBytesForRead(ref reader);
                        id.ChannelId = reader.GetString();
                        break;
                    }
                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("videoId")))
                    {
                        EnsureBytesForRead(ref reader);
                        id.VideoId = reader.GetString();
                        break;
                    }
                    break;
                default:
                    break;
            }
        }

        return id;
    }

    void processItems(ref Utf8JsonReader reader)
    {
        YouTubeItem youTubeItem = null;

        while (EnsureBytesForRead(ref reader) && reader.TokenType != JsonTokenType.EndArray)
        {

            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    youTubeItem = new YouTubeItem();
                    ytItems.Add(youTubeItem);
                    break;
                case JsonTokenType.PropertyName:
                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("kind")))
                    {
                        EnsureBytesForRead(ref reader);

                        youTubeItem.Kind = reader.GetString();
                        break;
                    }
                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("etag")))
                    {
                        EnsureBytesForRead(ref reader);

                        youTubeItem.ETag = reader.GetString();
                        break;
                    }
                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("id")))
                    {
                        EnsureBytesForRead(ref reader);

                        youTubeItem.Id = ProcessId(ref reader);
                        break;
                    }
                    break;
                default:
                    break;
            }
        }
    }

    object processPageInfo(ref Utf8JsonReader reader)
    {
        var needsRead = true;
        while (needsRead && EnsureBytesForRead(ref reader))
        {

            switch (reader.TokenType)
            {
                case JsonTokenType.EndObject:
                    needsRead = false;
                    break;
                case JsonTokenType.PropertyName:
                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("totalResults")))
                    {
                        EnsureBytesForRead(ref reader);
                        reader.GetInt32();
                        break;
                    }
                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("resultsPerPage")))
                    {
                        EnsureBytesForRead(ref reader);
                        reader.GetInt32();
                        break;
                    }

                    break;
                default:
                    break;
            }
        }

        return new { };
    }

    try
    {
        bool needsRead = true;
        while (needsRead && EnsureBytesForRead(ref reader))
        {

            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("items")))
                    {
                        processItems(ref reader);
                        Console.WriteLine($"ending item count: {ytItems.Count}");

                        break;
                    }
                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("etag")))
                    {
                        EnsureBytesForRead(ref reader);
                        Console.WriteLine($"etag is: {reader.GetString()}");
                        break;
                    }

                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("someOtherProperty")))
                    {
                        EnsureBytesForRead(ref reader);
                        Console.WriteLine($"someOtherProperty is: {reader.GetString()}");
                        break;
                    }

                    if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("pageInfo")))
                    {
                        // This is hacked to get through this object
                        processPageInfo(ref reader);

                        break;
                    }
                    break;
                case JsonTokenType.EndObject:
                    needsRead = false;
                    break;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Buffer at exception is: {Encoding.UTF8.GetString(buffer)}");
        throw ex;
    }
}

static void GetMoreBytesFromStream(FileStream stream, byte[] buffer, ref Utf8JsonReader reader)
{
    int bytesRead;
    if (reader.BytesConsumed < buffer.Length)
    {
        ReadOnlySpan<byte> leftover = buffer.AsSpan((int)reader.BytesConsumed);

        if (leftover.Length == buffer.Length)
        {
            Array.Resize(ref buffer, buffer.Length * 2);
            Console.WriteLine($"Increased buffer size to {buffer.Length}");
        }

        leftover.CopyTo(buffer);
        bytesRead = stream.Read(buffer.AsSpan(leftover.Length));
    }
    else
    {
        bytesRead = stream.Read(buffer);
    }
    //Console.WriteLine("More bytes retrieved");
    // Console.WriteLine($"Updated String in buffer is: {Encoding.UTF8.GetString(buffer)}");
    reader = new Utf8JsonReader(buffer, isFinalBlock: bytesRead == 0, reader.CurrentState);
}