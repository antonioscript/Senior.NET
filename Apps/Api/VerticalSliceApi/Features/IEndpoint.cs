namespace VerticalSliceApi.Features;

// Contract that every feature's endpoint class implements.
// The auto-discovery in EndpointExtensions scans the assembly for types implementing this
// interface and calls MapEndpoint on each — so adding a new feature requires zero changes to
// Program.cs or any registration file. Just create the class and it's live.
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
