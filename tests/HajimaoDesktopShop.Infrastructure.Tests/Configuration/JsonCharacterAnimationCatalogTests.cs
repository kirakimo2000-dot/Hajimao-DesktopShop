using HajimaoDesktopShop.Infrastructure.Configuration;
using System.Text.Json.Nodes;

namespace HajimaoDesktopShop.Infrastructure.Tests.Configuration;

public sealed class JsonCharacterAnimationCatalogTests
{
    [Fact]
    public async Task LoadAsync_ShippedMaomaoCatalog_HasValidatedRigSkinAndTenClips()
    {
        var testData = Path.Combine(AppContext.BaseDirectory, "TestData");
        var catalog = new JsonCharacterAnimationCatalog(
            Path.Combine(testData, "character-rigs.json"),
            Path.Combine(testData, "character-clips.json"),
            Path.Combine(testData, "character-skins.json"));

        var content = await catalog.LoadAsync();

        var rig = Assert.Single(content.Rigs);
        Assert.Equal("humanoid-v1", rig.Id);
        Assert.Contains(rig.Bones, bone => bone.Id == "root" && bone.ParentId is null);
        Assert.Contains(rig.Sockets, socket => socket.Id == "product_socket");

        var skin = Assert.Single(content.Skins);
        Assert.Equal("maomao-default", skin.Id);
        Assert.Equal(rig.Id, skin.RigId);

        Assert.Equal(10, content.Clips.Count);
        Assert.All(content.Clips, clip =>
        {
            Assert.Equal(rig.Id, clip.RigId);
            Assert.Equal(24, clip.LogicalFrameCount);
            Assert.NotEmpty(clip.BoneTracks);
        });
        Assert.Contains(
            content.Clips.Single(clip => clip.Id == "maomao-throw").Markers,
            marker => marker.Id == "release_product");
    }

    [Fact]
    public async Task LoadAsync_RejectsCatalogWithoutTheSharedHumanoidRig()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "TestData");
        var temporary = Path.Combine(Path.GetTempPath(), $"hajimao-character-contract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporary);
        try
        {
            foreach (var name in new[] { "character-rigs.json", "character-clips.json", "character-skins.json" })
            {
                var text = await File.ReadAllTextAsync(Path.Combine(source, name));
                await File.WriteAllTextAsync(
                    Path.Combine(temporary, name),
                    text.Replace("humanoid-v1", "alternate-rig", StringComparison.Ordinal));
            }

            var catalog = new JsonCharacterAnimationCatalog(
                Path.Combine(temporary, "character-rigs.json"),
                Path.Combine(temporary, "character-clips.json"),
                Path.Combine(temporary, "character-skins.json"));

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => catalog.LoadAsync());
            Assert.Contains("humanoid-v1", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_RejectsSharedHumanoidRigWithoutARequiredHeadBone()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "TestData");
        var temporary = Path.Combine(Path.GetTempPath(), $"hajimao-character-bones-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporary);
        try
        {
            var rigs = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(source, "character-rigs.json")))!;
            var bones = rigs["rigs"]![0]!["bones"]!.AsArray();
            bones.Remove(bones.Single(bone => bone!["id"]!.GetValue<string>() == "head"));
            await File.WriteAllTextAsync(Path.Combine(temporary, "character-rigs.json"), rigs.ToJsonString());

            var clips = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(source, "character-clips.json")))!;
            foreach (var clip in clips["clips"]!.AsArray())
            {
                clip!["boneTracks"]!.AsObject().Remove("head");
            }
            await File.WriteAllTextAsync(Path.Combine(temporary, "character-clips.json"), clips.ToJsonString());

            var skins = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(source, "character-skins.json")))!;
            skins["skins"]![0]!["parts"]!.AsObject().Remove("head");
            await File.WriteAllTextAsync(Path.Combine(temporary, "character-skins.json"), skins.ToJsonString());

            var catalog = new JsonCharacterAnimationCatalog(
                Path.Combine(temporary, "character-rigs.json"),
                Path.Combine(temporary, "character-clips.json"),
                Path.Combine(temporary, "character-skins.json"));

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => catalog.LoadAsync());
            Assert.Contains("head", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }
}
