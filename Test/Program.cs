// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using Spectre.Console;
using System.Text;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var hello = "[maroon on blue]Hello[/] " + Emoji.Known.GlobeShowingEuropeAfrica;
StringBuilder stringBuilder = new StringBuilder();
stringBuilder.AppendLine("[maroon on blue]3[/]");
stringBuilder.AppendLine("[red]:heart_suit:[/]");
var panel = new Panel(stringBuilder.ToString());
panel.Border = BoxBorder.Rounded;
AnsiConsole.MarkupLine(":heart_suit:");
AnsiConsole.MarkupLine(hello);
var cards = new Columns(panel, panel);
cards.Expand = false;
cards.Padding = new Padding(0, 0, 0, 0);
var showTable = new Rows(new Panel("hhh"), cards);
var panleTable = new Panel(showTable);
var json = JsonConvert.SerializeObject(panleTable);
AnsiConsole.Write(panleTable);

var table = new Table();

// Add some columns
table.AddColumn("3");
table.AddColumn("3");

// Add some rows
table.AddRow("Baz", "[green]Qux[/]");
table.AddRow(new Markup("[blue]Corgi[/]"), new Panel("Waldo"));
AnsiConsole.Write(table);

AnsiConsole.Write(
    new FigletText("GAME START")
        .LeftJustified()
        .Color(Color.Red));