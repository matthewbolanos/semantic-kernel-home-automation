using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Options;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;
using Q42.HueApi.ColorConverters.Original;

#nullable enable

namespace Plugins;

public class HueLights
{
    private LocalHueClient client;

    public bool IsOn { get; set; } = false;

    public HueLights(HueBridgeOptions hueBridgeOptions)
    {
        client = new LocalHueClient(hueBridgeOptions.IpAddress, hueBridgeOptions.ApiKey);
    }

    [KernelFunction("GetLights")]
    [Description("Gets a comma-separated list of light IDs")]
    public async Task<string> GetLightsAsync()
    {
        // Get the list of lights from the bridge
        var lights = await client.GetLightsAsync().ConfigureAwait(false);

        // Return the list of light IDs
        return "IDs: " + lights.Select(light => light.Id).Aggregate((a, b) => $"{a}, {b}");
    }

    [KernelFunction("GetState")]
    [Description("Gets the state of a particular light")]
    public async Task<LightStateModel> GetStateAsync(string id)
    {
        // Get the state of the light with the specified ID
        var light = await client.GetLightAsync(id).ConfigureAwait(false);

        // Return the simplified state of the light
        return new LightStateModel
        {
            On = light?.State.On,
            Brightness = light?.State.Brightness,
            Hex = light?.State.ToHex("LLC013")
        };
    }


    [KernelFunction("ChangeState")]
    [Description("Changes the state of the light; returns null if the light does not exist. The state properties will not be updated if they are null. The hue value ranges from 0 to 65535")]
    public async Task<LightStateModel?> ChangeStateAsync(string id, LightStateModel lightStateModel)
    {
        // Get the state of the light with the specified ID
        var light = await client.GetLightAsync(id).ConfigureAwait(false);

        if (light == null)
        {
            return null;
        }

        RGBColor? rGBColor = lightStateModel.Hex != null ? new(lightStateModel.Hex) : null;

        // Send the updated state to the light
        await client.SendCommandAsync(new LightCommand
        {
            On = lightStateModel.On ?? light.State.On,
            Brightness = lightStateModel.Brightness ?? light.State.Brightness,
            Hue = (int?)rGBColor?.GetHue(),
            Saturation = (int?)rGBColor?.GetSaturation()
        }, new List<string> { id }).ConfigureAwait(false);

        // Return the updated state of the light
        return await GetStateAsync(id).ConfigureAwait(false);
    }
}

public class LightStateModel
{
    public bool? On { get; set; }
    public byte? Brightness { get; set; }
    public string? Hex { get; set; }
}