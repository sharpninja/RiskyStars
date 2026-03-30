using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class RiskyStarsGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private GrpcGameClient? _gameClient;
    private GameStateCache? _gameStateCache;

    public RiskyStarsGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _gameClient = new GrpcGameClient("http://localhost:5000");
        _gameStateCache = new GameStateCache();
        
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (_gameClient != null && _gameClient.IsConnected)
        {
            var updates = _gameClient.DequeueAllUpdates();
            foreach (var update in updates)
            {
                ProcessGameUpdate(update);
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        base.Draw(gameTime);
    }

    private void ProcessGameUpdate(GameUpdate update)
    {
        if (_gameStateCache == null)
        {
            return;
        }

        _gameStateCache.ApplyUpdate(update);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gameClient?.Dispose();
        }
        base.Dispose(disposing);
    }
}
