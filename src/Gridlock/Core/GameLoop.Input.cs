using Gridlock.Towers;

namespace Gridlock.Core
{
    public sealed partial class GameLoop
    {
        private void HandleGlobalInput()
        {
            if (_input.SpacePressed)
            {
                if (_gameManager.CurrentState == GameState.Preparing)
                {
                    _gameManager.SetState(GameState.Wave);
                    _gameStats.SetWave(_waveManager.CurrentWave + 1);
                }
                else if (_gameManager.CurrentState == GameState.GameOver)
                {
                    ResetGame();
                }
            }

            if (_input.EscapePressed)
            {
                if (_modPanel.IsOpen)
                    _modPanel.Close();
                else if (_gameManager.CurrentState == GameState.GameOver)
                    ResetGame();
            }

            if (_input.RightClicked && _modPanel.IsOpen)
                _modPanel.Close();
        }

        private void HandlePlacementInput()
        {
            bool panelOpen = _modPanel != null && _modPanel.IsOpen;
            bool imguiWantsMouse = ImGuiNET.ImGui.GetIO().WantCaptureMouse;

            if (_camera.ScreenToGroundPoint(_input.MouseScreenPos, out var groundPoint))
            {
                if (!panelOpen)
                    _towerPlacement.UpdatePreview(groundPoint);

                var newHover = imguiWantsMouse ? null : _towerPlacement.TryClickTower(groundPoint);
                if (newHover != _hoveredTower)
                {
                    _hoveredTower = newHover;
                    if (_hoveredTower != null)
                        _soundManager.Play(Audio.SoundType.TowerHover, worldPos: _hoveredTower.Position);
                }

                if (_input.LeftClicked && _gameManager.CurrentState == GameState.Preparing && !imguiWantsMouse)
                {
                    if (panelOpen)
                    {
                        _modPanel.Close();
                        _selectedTower = null;
                    }
                    else
                    {
                        var clickedTower = _towerPlacement.TryClickTower(groundPoint);
                        if (clickedTower != null)
                        {
                            _selectedTower = clickedTower;
                            _modPanel.Open(clickedTower, _inventory);
                        }
                        else
                        {
                            _towerPlacement.TryPlace(groundPoint, isOverUI: false);
                        }
                    }
                }
            }
            else
            {
                _hoveredTower = null;
            }
        }
    }
}
