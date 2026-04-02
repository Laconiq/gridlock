using Gridlock.Core;
using Raylib_cs;

Raylib.SetConfigFlags(ConfigFlags.VSyncHint | ConfigFlags.ResizableWindow);
Raylib.InitWindow(1920, 1080, "Gridlock");
Raylib.InitAudioDevice();

var game = new GameLoop();
game.Initialize();

while (!Raylib.WindowShouldClose())
    game.RunFrame();

game.Shutdown();
Raylib.CloseAudioDevice();
Raylib.CloseWindow();
