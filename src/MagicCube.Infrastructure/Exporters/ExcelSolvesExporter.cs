/*
 * Magic Cube Simulator
 * Copyright (C) 2026 Magic Cube Project
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using ClosedXML.Excel;
using MagicCube.Core.Models;
using MagicCube.Services;

namespace MagicCube.Infrastructure.Exporters
{
    public class ExcelSolvesExporter : ISolvesExporter
    {
        public async Task ExportAsync(IEnumerable<SolveRecord> records, string filePath, CancellationToken token = default)
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Solves");

                // Titles
                ws.Cell(1, 1).Value = "ID";
                ws.Cell(1, 2).Value = "Timestamp";
                ws.Cell(1, 3).Value = "Duration (s)";
                ws.Cell(1, 4).Value = "Scramble";
                ws.Cell(1, 5).Value = "Solution";

                int row = 2;
                foreach (var rec in records)
                {
                    ws.Cell(row, 1).Value = rec.Id.ToString();
                    ws.Cell(row, 2).Value = rec.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    ws.Cell(row, 3).Value = rec.Duration.TotalSeconds.ToString("F3");
                    ws.Cell(row, 4).Value = rec.Scramble;
                    ws.Cell(row, 5).Value = rec.Solution;
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }, token);
        }
    }
}