﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PhotoToys.Features;

class BasicManipulation : Category
{
    public override string Name { get; } = nameof(BasicManipulation).ToReadableName();
    public override string Description { get; } = "Apply basic image manipulation techniques!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new HSVManipulation(),
        new ImageBlending()
    };
}
class HSVManipulation : Feature
{
    enum ChannelName : int
    {
        Red = 2,
        Green = 1,
        Blue = 0,
        Alpha = 3
    }
    public override string Name { get; } = $"HSV {nameof(HSVManipulation)[3..].ToReadableName()}";
    public override IEnumerable<string> Allias => new string[] { "HSV", "Hue", "Saturation", "Value", "Brightness", "Color", "Change Color" };
    public override string Description { get; } = "Change Hue, Saturation, and Brightness of an image";
    static string Convert(double i) => i > 0 ? $"+{i:N0}" : i.ToString("N0");
    public HSVManipulation()
    {
        
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new DoubleSliderParameter("Hue Shift", -180, 180, 0, DisplayConverter: Convert).Assign(out var HueShiftParam),
                new DoubleSliderParameter("Saturation Shift", -100, 100, 0, DisplayConverter: Convert).Assign(out var SaturationShiftParam),
                new DoubleSliderParameter("Brightness Shift", -100, 100, 0, DisplayConverter: Convert).Assign(out var BrightnessShiftParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                Mat image = ImageParam.Result.Track(tracker);
                double hue = HueShiftParam.Result;
                double sat = SaturationShiftParam.Result / 100d;
                double bri = BrightnessShiftParam.Result / 100d;
                Mat output = new Mat().Track(tracker);
                var originalchannelcount = image.Channels();
                
                image = image.ToBGR(out var originalA).Track(tracker).CvtColor(ColorConversionCodes.BGR2HSV).Track(tracker);
                
                var outhue = (
                        image.ExtractChannel(0).Track(tracker).AsDoubles().Track(tracker) + hue / 2
                    ).Track(tracker).ToMat().Track(tracker);
                Cv2.Subtract(outhue, 180d, outhue, outhue.GreaterThan(180).Track(tracker));
                Cv2.Add(outhue, 180d, outhue, outhue.LessThan(0).Track(tracker));

                var outsat = (
                        image.ExtractChannel(1).Track(tracker).AsDoubles().Track(tracker) + sat * 255
                    ).Track(tracker).ToMat().Track(tracker);
                outsat.SetTo(0, mask: outsat.LessThan(0).Track(tracker));
                outsat.SetTo(255, mask: outsat.GreaterThan(255).Track(tracker));
                
                var outbright = (
                    image.ExtractChannel(2).Track(tracker).AsDoubles().Track(tracker) + bri * 2552
                ).Track(tracker).ToMat().Track(tracker);
                outbright.SetTo(0, mask: outbright.LessThan(0).Track(tracker));
                outbright.SetTo(255, mask: outbright.GreaterThan(255).Track(tracker));

                Cv2.Merge(new Mat[]
                {
                    outhue,
                    outsat,
                    outbright
                }, output);
                output = output.AsBytes().Track(tracker);
                output = output.CvtColor(ColorConversionCodes.HSV2BGR).Track(tracker);
                if (originalA != null)
                    output = output.InsertAlpha(originalA).Track(tracker);

                output.Clone().ImShow(MatImage);
            }
        );
    }
}
class ImageBlending : Feature
{
    enum ChannelName : int
    {
        Red = 2,
        Green = 1,
        Blue = 0,
        Alpha = 3
    }
    public override string Name { get; } = nameof(ImageBlending).ToReadableName();
    public override IEnumerable<string> Allias => new string[] { "2 Images", "Blend Image" };
    public override string Description { get; } = "Blend two images together";
    public ImageBlending()
    {
        
    }
    protected override UIElement CreateUI()
    {
        UIElement? Element = null;
        return Element = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter("Image 1").Assign(out var Image1Param),
                new ImageParameter("Image 2").Assign(out var Image2Param),
                new PercentSliderParameter("Percentage of Image 1", 0.5).Assign(out var Percent1Param)
            },
            OnExecute: async (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var image1 = Image1Param.Result.Track(tracker);
                var image2 = Image2Param.Result.Track(tracker);
                var percent1 = Percent1Param.Result;
                Mat output = new();
                if (image1.Width != image2.Width || image1.Height != image2.Height)
                {
                    if (Element != null)
                        await new ContentDialog
                        {
                            Title = "Error",
                            Content = "Both images must have the same size",
                            XamlRoot = Element.XamlRoot,
                            PrimaryButtonText = "Okay"
                        }.ShowAsync();
                    return;
                }

                Cv2.AddWeighted(image1, percent1, image2, 1 - percent1, 0, output);
                output.ImShow(MatImage);
            }
        );
    }
}
