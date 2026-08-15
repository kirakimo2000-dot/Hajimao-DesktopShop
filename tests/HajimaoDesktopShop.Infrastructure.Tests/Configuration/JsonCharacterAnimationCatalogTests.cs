using HajimaoDesktopShop.Infrastructure.Configuration;

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
}
