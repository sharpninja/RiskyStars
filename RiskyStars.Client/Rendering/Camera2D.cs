using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace RiskyStars.Client;

public class Camera2D
{
    private Vector2 _position;
    private float _zoom;
    private float _rotation;
    private int _viewportWidth;
    private int _viewportHeight;

    public const float MinimumZoom = 0.1f;
    public const float MaximumZoom = 5.0f;
    private const float SmoothSpeed = 0.1f;

    private Vector2? _lastMousePosition;
    private bool _isPanning;
    private Vector2? _targetPosition;
    private bool _isTracking;
    private int _previousScrollWheelValue;
    
    public float PanSpeed { get; set; } = 5.0f;
    public float ZoomSpeed { get; set; } = 0.1f;
    public bool InvertScrollZoom { get; set; }

    public Camera2D(int viewportWidth, int viewportHeight)
    {
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;
        _position = Vector2.Zero;
        _zoom = 1.0f;
        _rotation = 0f;
        _previousScrollWheelValue = Mouse.GetState().ScrollWheelValue;
    }

    public Matrix GetTransformMatrix()
    {
        return
            Matrix.CreateTranslation(new Vector3(-_position.X, -_position.Y, 0)) *
            Matrix.CreateRotationZ(_rotation) *
            Matrix.CreateScale(new Vector3(_zoom, _zoom, 1)) *
            Matrix.CreateTranslation(new Vector3(_viewportWidth * 0.5f, _viewportHeight * 0.5f, 0));
    }

    public void Update(GameTime gameTime)
    {
        var keyState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        bool userInput = false;

        float moveSpeed = PanSpeed / _zoom;
        if (keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift))
        {
            moveSpeed *= 3.0f;
        }

        if (keyState.IsKeyDown(Keys.W) || keyState.IsKeyDown(Keys.Up))
        {
            _position.Y -= moveSpeed;
            userInput = true;
        }
        if (keyState.IsKeyDown(Keys.S) || keyState.IsKeyDown(Keys.Down))
        {
            _position.Y += moveSpeed;
            userInput = true;
        }
        if (keyState.IsKeyDown(Keys.A) || keyState.IsKeyDown(Keys.Left))
        {
            _position.X -= moveSpeed;
            userInput = true;
        }
        if (keyState.IsKeyDown(Keys.D) || keyState.IsKeyDown(Keys.Right))
        {
            _position.X += moveSpeed;
            userInput = true;
        }

        if (mouseState.MiddleButton == ButtonState.Pressed)
        {
            if (!_isPanning && _lastMousePosition.HasValue)
            {
                var delta = new Vector2(mouseState.X, mouseState.Y) - _lastMousePosition.Value;
                _position -= delta / _zoom;
            }
            _isPanning = true;
            userInput = true;
        }
        else
        {
            _isPanning = false;
        }

        _lastMousePosition = new Vector2(mouseState.X, mouseState.Y);

        int scrollDelta = mouseState.ScrollWheelValue - _previousScrollWheelValue;
        if (scrollDelta != 0)
        {
            float wheelSteps = scrollDelta / 120f;
            if (InvertScrollZoom)
            {
                wheelSteps *= -1f;
            }

            var cursorScreenPosition = new Vector2(mouseState.X, mouseState.Y);
            var worldBeforeZoom = ScreenToWorld(cursorScreenPosition);

            float zoomFactorPerStep = MathF.Max(1.01f, 1f + (ZoomSpeed * 0.6f));
            _zoom = MathHelper.Clamp(_zoom * MathF.Pow(zoomFactorPerStep, wheelSteps), MinimumZoom, MaximumZoom);

            var worldAfterZoom = ScreenToWorld(cursorScreenPosition);
            _position += worldBeforeZoom - worldAfterZoom;
            userInput = true;
        }

        _previousScrollWheelValue = mouseState.ScrollWheelValue;

        if (keyState.IsKeyDown(Keys.OemPlus) || keyState.IsKeyDown(Keys.Add))
        {
            _zoom = MathHelper.Clamp(_zoom + ZoomSpeed * deltaTime, MinimumZoom, MaximumZoom);
        }

        if (keyState.IsKeyDown(Keys.OemMinus) || keyState.IsKeyDown(Keys.Subtract))
        {
            _zoom = MathHelper.Clamp(_zoom - ZoomSpeed * deltaTime, MinimumZoom, MaximumZoom);
        }

        if (userInput)
        {
            _isTracking = false;
            _targetPosition = null;
        }

        if (_isTracking && _targetPosition.HasValue)
        {
            _position = Vector2.Lerp(_position, _targetPosition.Value, SmoothSpeed);
            
            if (Vector2.Distance(_position, _targetPosition.Value) < 1f)
            {
                _isTracking = false;
                _targetPosition = null;
            }
        }
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        var transform = GetTransformMatrix();
        Matrix.Invert(ref transform, out var invertedMatrix);
        return Vector2.Transform(screenPosition, invertedMatrix);
    }

    public void CenterOn(Vector2 position)
    {
        _position = position;
        _isTracking = false;
        _targetPosition = null;
    }

    public void SmoothCenterOn(Vector2 position)
    {
        _targetPosition = position;
        _isTracking = true;
    }

    public void SetZoom(float zoom)
    {
        _zoom = MathHelper.Clamp(zoom, MinimumZoom, MaximumZoom);
    }

    public void SetView(Vector2 position, float zoom)
    {
        _position = position;
        SetZoom(zoom);
        _isTracking = false;
        _targetPosition = null;
    }

    public void PanByScreenDelta(Vector2 screenDelta)
    {
        _position -= screenDelta / _zoom;
        _isTracking = false;
        _targetPosition = null;
    }

    public void ResizeViewport(int viewportWidth, int viewportHeight)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return;
        }

        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;
    }

    public Vector2 Position => _position;
    public float Zoom => _zoom;
}
