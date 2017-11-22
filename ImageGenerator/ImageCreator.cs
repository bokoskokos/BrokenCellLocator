using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ImageGenerator
{
    class ImageCreator : IDisposable
    {
        private const int MaxPannelSize = 220;
        private const int MinPannelSize = 100;
        private const int MaxPannelX = 20;
        private const int MinPannelX = -20;
        private const int MaxPannelY = 20;
        private const int MinPannelY = -20;

        private readonly SKColor _healthyCellCollor = SKColors.Black;
        private readonly SKColor _deadCellCollor = SKColors.OrangeRed;

        private List<List<(int, int)>> _allBrokenCells = new List<List<(int, int)>>();

        public void Create(int numberOfFiles)
        {
            _allBrokenCells = new List<List<(int, int)>>();
            for (var i = 0; i < numberOfFiles; i++)
                Create(Path.Combine("img", $"File_{i.ToString().PadLeft(6, '0')}.jpg"));

            CreateReport();
        }

        public void Dispose()
        {
        }

        private void Create(string fileName)
        {
            var rand = new Random();
            float size = rand.Next(MinPannelSize, MaxPannelSize);
            float x = rand.Next(MinPannelX, MaxPannelX);
            float y = rand.Next(MinPannelY, MaxPannelY);

            var bitmap = new SKBitmap(Program.FrameSize, Program.FrameSize, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            var surface = SKSurface.Create(bitmap.Info);

            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.DrawRect(new SKRect(left: x, top: y, right: x + size, bottom: y + size), new SKPaint {Color = SKColors.Blue });

            float padding = 1;
            float cellWidth = (size - 11 * padding) / 10;
            float cellHeight = (size - 6 * padding) / 5;

            var brokenCells = new List<(int, int)>();

            for (var i = 0; i < 10; i++)
            {
                var currX = x + i * cellWidth + (padding * (i + 1));
                for (var j = 0; j < 5; j++)
                {
                    var currY = y + j * cellHeight + (padding*(j+1));
                    var cellHealth = rand.Next(20); // 1 in 20 chance
                    var cellColour = cellHealth == 10 ? _deadCellCollor : _healthyCellCollor;

                    if (cellHealth == 10)
                    {
                        // need to see if cell is visible
                        var intersect = Rectangle.Intersect(
                            new Rectangle(x: (int)currX, y: (int)currY, width: (int)cellWidth, height: (int)cellHeight),
                            new Rectangle(x: 0, y: 0, width: Program.FrameSize, height: Program.FrameSize));
                        if (intersect.Width > 0 && intersect.Height > 0)
                            brokenCells.Add((i, j));
                    }

                    canvas.DrawRect(new SKRect(left: currX, top: currY, right: currX + cellWidth, bottom: currY + cellHeight), new SKPaint { Color = cellColour });
                }
            }

            var data = surface.Snapshot().Encode(SKEncodedImageFormat.Jpeg, 100);
            var ms = new MemoryStream();
            data.SaveTo(ms);
            var bytes = ms.ToArray();

            File.WriteAllBytes(fileName, bytes);

            _allBrokenCells.Add(brokenCells);
        }

        private void CreateReport()
        {
            var json = JsonConvert.SerializeObject(_allBrokenCells);
            var path = Path.Combine("img", "report.json");

            if (!File.Exists(path))
                File.Delete(path);

            File.WriteAllText(path, json);
        }
    }
}
