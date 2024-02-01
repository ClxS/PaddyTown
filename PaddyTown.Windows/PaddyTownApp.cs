using Stride.Engine;

namespace PaddyTown
{
    class PaddyTownApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
