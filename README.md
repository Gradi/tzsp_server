# TZSP Server

This is naive implementation for
[TZSP](https://en.wikipedia.org/wiki/TZSP). You can use it to analyze
network packets (although, it's better to use Wireshark).

## Installation

- Install .NET 5
- Now you can run server with command

```
dotnet run --project .\TzspServer\TzspServer.csproj -c Release -- --help
```

## How to do it

- Create new empty classlib project (.NET 5) (1)
- Reference `TzspServerAnalyzerApi\TzspServerAnalyzerApi.csproj` project
- Add one or more classes each implementing `IAnalyzer` or  `PacketFilterAnalyzer`
- Put `AnalyzersOrderAttribute` attribute on assembly and pass
  `typeof`'s of newly created classes (order is important)
- Build project
- Run server with command

```
dotnet run --project .\TzspServer\TzspServer.csproj -c Release -- -a <path-to-assembly-1>
```

If everything is fine then server will start, load your assembly and
every packet server receieves will be unpacked and sent to
`IAnalyzer.Handle` method of your classes in order you specified in
`AnalyzersOrderAttribute`. You can return `AResult.Stop()` to not to
send packet to next analyzers (use `AResult.Continue()` to send packet
to next analyzers in chain).

You can edit project/assembly without necessary to restart server. It
will detect assembly change and reload it automatically.

It is pointless to attempt to alter packet payload, because server
receives copies and doesn't send any response at all.

## Where to get TZSP client

I don't know :). Contact your router for that functionality.

PS. MikroTik users visit this [link](https://wiki.mikrotik.com/wiki/Manual:Tools/Packet_Sniffer)
