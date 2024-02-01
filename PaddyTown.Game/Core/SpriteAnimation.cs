using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Rendering.Sprites;

namespace PaddyTown.Core
{    
    public class SpriteAnimation : SyncScript
    {
       // Declared public member fields and properties are displayed in Game Studio.
       private SpriteFromSheet sprite;
       private DateTime lastFrame;
       
       public float UpdateDelaySeconds { get; } = 0.1f;
    
       public override void Start()
       {
           // Initialize the script.
           sprite = Entity.Get<SpriteComponent>().SpriteProvider as SpriteFromSheet;
           lastFrame = DateTime.Now;
       }
    
       public override void Update()
       {
          // Do something every new frame.
          if ((DateTime.Now - lastFrame) > TimeSpan.FromSeconds(this.UpdateDelaySeconds))
          {
             sprite.CurrentFrame += 1;
             lastFrame = DateTime.Now;
          }
       }
    }
}