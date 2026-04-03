using Gridlock.Core;
using Raylib_cs;

bool benchmark = args.Contains("--benchmark");
bool profile = args.Contains("--profile");
bool screenshot = args.Contains("--screenshot");
int benchFrames = 0;
double benchSeconds = 30;

for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--frames")
        int.TryParse(args[i + 1], out benchFrames);
    if (args[i] == "--seconds")
        double.TryParse(args[i + 1], out benchSeconds);
}

var flags = ConfigFlags.ResizableWindow;
if (!benchmark)
    flags |= ConfigFlags.VSyncHint;

Raylib.SetConfigFlags(flags);
Raylib.InitWindow(1920, 1080, "Gridlock");
Raylib.InitAudioDevice();

var profiler = Profiler.Instance;
if (benchmark || profile)
{
    profiler.Enabled = true;
    profiler.EnableCsvLog("profile.csv");
}

var game = new GameLoop();
game.Initialize();

if (benchmark || screenshot)
    game.StartBenchmark();

int frame = 0;
var benchSw = System.Diagnostics.Stopwatch.StartNew();
while (!Raylib.WindowShouldClose())
{
    if (screenshot && frame == 120)
        game.RequestScreenshot("screenshot.png");

    game.RunFrame();
    frame++;

    if (benchmark)
    {
        if (benchFrames > 0 && frame >= benchFrames) break;
        if (benchFrames == 0 && benchSw.Elapsed.TotalSeconds >= benchSeconds) break;
    }
    if (screenshot && frame >= 130)
        break;
}

if (profiler.Enabled)
{
    var report = profiler.GenerateReport();
    Console.WriteLine(report);
    File.WriteAllText("profile_report.txt", report);
    profiler.Shutdown();
}

game.Shutdown();
Raylib.CloseAudioDevice();
Raylib.CloseWindow();
