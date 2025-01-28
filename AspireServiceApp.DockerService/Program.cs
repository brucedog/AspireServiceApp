using Docker.DotNet;
using Docker.DotNet.Models;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("*");
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseCors();

// this does not support hot reload
app.MapGet("/GetAllDockerContainerConfiguration", async () =>
    {
        return await GetAllDockerContainerConfiguration();
        // var statuses = new List<string>();
        // var ids = await GetDockerIds();
        // foreach (string id in ids)
        // {
        //     var status = await GetDockerContainerStatus(id);
        //     Console.WriteLine($"Container {id} is {status}");
        //     statuses.Add($"Container {id} is {await GetDockerContainerStatus(id)}");
        // }
        // return statuses;
    })
    .WithName("DockerService")
    .RequireCors(MyAllowSpecificOrigins);

app.MapDefaultEndpoints();

app.Run();

async Task<List<string>> GetDockerIds()
{
    List<string> ids = new List<string>();

    using var client = new DockerClientConfiguration().CreateClient();
    client.DefaultTimeout = TimeSpan.FromSeconds(10);
    var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
    foreach (var container in containers)
    {
        ids.Add(container.ID);
    }

    return ids;
}

async Task<bool> PullImageIfHashMismatch(string imageName)
{
    using var client = new DockerClientConfiguration().CreateClient();
    var images = await client.Images.ListImagesAsync(new ImagesListParameters() { All = true });
    var localImage = images.FirstOrDefault(i => i.RepoTags.Contains(imageName));

    if (localImage == null)
    {
        // Image not found locally, pull it
        await client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = imageName }, null, new Progress<JSONMessage>());
        return true;
    }

    var localImageHash = localImage.ID;
    var remoteImageHash = await GetRemoteImageHash(imageName);

    if (localImageHash != remoteImageHash)
    {
        // Hashes do not match, pull the image
        await client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = imageName }, null, new Progress<JSONMessage>());
        return true;
    }

    return false;
}

async Task<string> GetRemoteImageHash(string imageName)
{
    using var client = new DockerClientConfiguration().CreateClient();
    var images = await client.Images.ListImagesAsync(new ImagesListParameters() { All = true });
    var remoteImage = images.FirstOrDefault(i => i.RepoTags.Contains(imageName));

    return remoteImage?.ID;
}

async Task<string> GetDockerContainerStatus(string containerId)
{
    // This was generated from CoPilot and does not work on my Windows box
    //using (var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient())

    using var client = new DockerClientConfiguration().CreateClient();
    var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
    var container = containers.FirstOrDefault(c => c.ID == containerId);
        
    if (container == null)
    {
        return "Container not found";
    }

    return container.State;
}

async Task<List<DockerContainerInfo>> GetAllDockerContainerConfiguration()
{
    using var client = new DockerClientConfiguration().CreateClient();
    List<DockerContainerInfo> containerList = new List<DockerContainerInfo>();
    var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
    foreach (ContainerListResponse container in containers)
    {
        containerList.Add(
            new DockerContainerInfo
            {
                Id = container.ID,
                State = container.State,
                Image = container.Image,
                Name = container.Names.FirstOrDefault(),
                Created = container.Created
            });
    }

    return containerList;
}

async Task<DockerContainerInfo> GetDockerContainerConfiguration(string containerId)
{
    using var client = new DockerClientConfiguration().CreateClient();
    var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
    var container = containers.FirstOrDefault(c => c.ID == containerId);

    if (container == null)
    {
        return null;
    }

    return new DockerContainerInfo
    {
        Id = container.ID,
        State = container.State,
        Image = container.Image,
        Name = container.Names.FirstOrDefault(),
        Created = container.Created
    };
}