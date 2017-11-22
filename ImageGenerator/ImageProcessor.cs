using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ImageGenerator
{
    public class ImageProcessor : IDisposable
    {
        private bool _debug = false;
        public void Process(bool debug = false)
        {
            _debug = debug;
            var allImages = Directory.GetFiles("img", "*.jpg").ToList();
            var index = 0;
            allImages.Sort();
            foreach (var i in allImages)
            {
                using (var image = new Bitmap(Image.FromFile(i)))
                {
                    var edges = CheckEdges(image);

                    Logger.Debug($"{i}: \t{edges.top} \t{edges.right} \t{edges.bottom} \t{edges.left}");

                    if ((edges.top || edges.bottom) && (edges.left || edges.right))
                    {
                        var start = FindPannelStartCoordinates(image, edges.top, edges.left);
                        Logger.Debug($"x: {start.x} \t y: {start.y}");

                        var size = GetCellSize(image, start, edges.top, edges.left);
                        Logger.Debug($"width: {size.width} \theight: {size.height}");

                        var brokenCells = GetBrokenCells(image, start, edges.top, edges.left, size);
                        var brokenCellsMessage = $"{i} \tBroken Cells: ";
                        foreach (var bc in brokenCells)
                            brokenCellsMessage += $"[{bc.x}][{bc.y}],";

                        brokenCellsMessage.TrimEnd(',');
                        Logger.Debug(brokenCellsMessage);

                        Logger.Report(index++, brokenCells.ToArray());
                    }
                    else
                    {
                        Logger.Report(index++);
                        counter++;
                        Logger.Debug("Cannot proccess");
                    }
                }
            }
        }

        #region Image Helpers

        private List<(int x, int y)> GetBrokenCells(Bitmap image, (int x, int y) start, bool down, bool right, (int width, int height) size)
        {
            var brokenCells = new List<(int x, int y)>();

            bool xLoopCondition(int value) => right ? value < Program.FrameSize : value >= 0;
            void xLoopIncrement(ref int value) => value += right ? 1: -1;

            var startX = start.x + (right ? 1 + size.width / 2 : -1 - size.width / 2);
            var startY = start.y + (down ? 1 + size.height / 2 : -1 - size.height / 2);

            var x = startX;
            var y = startY;

            var i = right ? 0 : 9;

            var initialBrokenCellYs = ScanVertical(image, x, down, startY);

            foreach (var brokenY in initialBrokenCellYs)
            {
                brokenCells.Add((x: i, y: brokenY));
            }

            var atBorder = false;
            xLoopIncrement(ref x);
            while (xLoopCondition(x))
            {
                var colour = image.GetPixel(x, y);
                image.SetPixel(x, y, Color.Yellow);
                if (IsBlack(colour) || IsRedOrange(colour))
                {
                    if (atBorder)
                    {
                        i += right ? 1 : -1;
                        atBorder = false;
                        var brokenCellYs = ScanVertical(image, x, down, startY);

                        foreach (var brokenY in brokenCellYs)
                            brokenCells.Add((x: i, y: brokenY));
                    }
                }
                else if (IsWhite(colour))
                    break;
                else
                    atBorder = true;

                xLoopIncrement(ref x);
            }

            if (_debug)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    var path = Path.Combine("img", $"File_{counter++.ToString().PadLeft(6, '0')}_dots.jpg");
                    if (File.Exists(path))
                        File.Delete(path);

                    File.WriteAllBytes(path, ms.ToArray());
                }
            }

            return brokenCells;
        }

        private int[] ScanVertical(Bitmap image, int x, bool down, int startY)
        {
            var brokenCells = new List<int>();

            var loopStart = true;

            bool yLoopCondition(int value) => down ? value < Program.FrameSize : value >= 0;
            void yLoopIncrement(ref int value) => value += down ? 1 : -1;
            var y = startY;
            var atBorder = false;
            var j = down ? 0 : 4;
            yLoopIncrement(ref y);
            while (yLoopCondition(y))
            {
                var colour = image.GetPixel(x, y);
                image.SetPixel(x, y, Color.Yellow);
                if (IsBlack(colour))
                {
                    if (atBorder)
                    {
                        atBorder = false;
                        j += down ? 1 : -1;
                    }
                }
                else if (IsRedOrange(colour))
                {
                    if (atBorder)
                    {
                        atBorder = false;
                        j += down ? 1 : -1;
                        brokenCells.Add(j);
                    }
                    else if (loopStart)
                    {
                        brokenCells.Add(j);
                    }
                }
                else if (IsWhite(colour))
                    break;
                else
                    atBorder = true;

                loopStart = false;
                yLoopIncrement(ref y);
            }

            return brokenCells.ToArray();
        }

        private static int counter = 0;

        private (int width, int height) GetCellSize(Bitmap image, (int x, int y) start, bool down, bool right)
        {
            bool LoopConditions(int i, int j) => (right ? i < Program.FrameSize : i >= 0) && (down ? j < Program.FrameSize : j >= 0);
            void LoopIncrement(ref int i, ref int j)
            {
                i += right? 1 : -1;
                j += down ? 1 : -1;
            }

            for (int i = start.x, j = start.y; LoopConditions(i, j); LoopIncrement(ref i, ref j))
            {
                if (i < 0 || j < 0)
                {
                    return (0,0);
                }
                var colour = image.GetPixel(i, j);
                if (IsBlack(colour) || IsRedOrange(colour))
                {
                    // adjust for travelling too far
                    var horizontal = i;
                    while (horizontal > 0 && horizontal < Program.FrameSize)
                    {
                        var c = image.GetPixel(horizontal + (right ? -1 : 1), j);
                        if (!IsBlack(c) && !IsRedOrange(c))
                            break;
                        horizontal += right ? -1 : 1;
                    }

                    var vertical = j;
                    while (vertical > 0 && vertical < Program.FrameSize)
                    {
                        var c = image.GetPixel(i, vertical + (down ? -1 : 1));
                        if (!IsBlack(c) && !IsRedOrange(c))
                            break;
                        vertical += (down ? -1 : 1);
                    }

                    var width = 0;
                    while (width < Program.FrameSize)
                    {
                        var p = image.GetPixel(horizontal + (right ? width : -width), j);
                        if (!IsBlack(p) && !IsRedOrange(p))
                            break;
                        width++;
                    }

                    var height = 0;
                    while (vertical < Program.FrameSize)
                    {
                        var p = image.GetPixel(i, vertical + (down ? height : -height));
                        if (!IsBlack(p) && !IsRedOrange(p))
                            break;
                        height++;
                    }

                    return (width, height);
                }
            }

            return (0, 0);
        }

        private (int x, int y) FindPannelStartCoordinates(Bitmap image, bool down, bool right)
        {
            var x = -1;
            var y = -1;

            for (var i = 0; i < Program.FrameSize; i++)
            {
                var horizontalIncrement = right ? i : Program.FrameSize - 1 - i;
                var verticalIncrement = down ? i : Program.FrameSize - 1 - i;

                if (x == -1 && (!IsWhite(image.GetPixel(horizontalIncrement, Program.FrameSize / 3)) || !IsWhite(image.GetPixel(horizontalIncrement, Program.FrameSize / 3 * 2))))
                    x = horizontalIncrement;
                if (y == -1 && (!IsWhite(image.GetPixel(Program.FrameSize / 3, verticalIncrement)) || !IsWhite(image.GetPixel(Program.FrameSize / 3 * 2, verticalIncrement))))
                    y = verticalIncrement;
                if (x != -1 && y != -1)
                    break;
            }

            return (x, y);
        }

        private (bool top, bool right, bool bottom, bool left) CheckEdges(Bitmap image)
        {
            var topEdgeHasFrame = true;
            var rightEdgeHasFrame = true;
            var bottomEdgeHasFrame = true;
            var leftEdgeHasFrame = true;

            for (var i = 0; i < Program.FrameSize; i++)
            {
                if (topEdgeHasFrame)
                    topEdgeHasFrame = IsWhite(image.GetPixel(i, 0));
                if (rightEdgeHasFrame)
                    rightEdgeHasFrame = IsWhite(image.GetPixel(Program.FrameSize - 1, i));
                if (bottomEdgeHasFrame)
                    bottomEdgeHasFrame = IsWhite(image.GetPixel(i, Program.FrameSize - 1));
                if (leftEdgeHasFrame)
                    leftEdgeHasFrame = IsWhite(image.GetPixel(0, i));
            }

            return (top: topEdgeHasFrame, right: rightEdgeHasFrame, bottom: bottomEdgeHasFrame, left: leftEdgeHasFrame);
        }

        #endregion

        #region Colour Helpers

        private bool IsWhite(Color colour)
        {
            const int tollerance = 65;
            bool InRange(int x) => x > 255 - tollerance;
            return InRange(colour.R) && InRange(colour.G) && InRange(colour.B);
        }

        private bool IsBlack(Color colour)
        {
            const int tollerance = 40;
            bool InRange(int x) => x <= tollerance;
            var hue = colour.GetHue();
            return InRange(colour.R) && InRange(colour.G) && InRange(colour.B);
        }

        private bool IsRedOrange(Color colour)
        {
            var hue = colour.GetHue();
            var brightness = colour.GetBrightness();

            return (hue > 0 && hue < 45) || (hue > 330 && hue <= 360) && brightness > 80;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}
