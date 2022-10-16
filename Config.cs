using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using NoteCutGuide.Algorithm;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace NoteCutGuide
{
    internal class Config
    {
        public static Config Instance;
		public virtual bool Enabled { get; set; } = true;
		public virtual float Width { get; set; } = 0.05f;
		public virtual float Height { get; set; } = 0.5f;
		public virtual float Depth { get; set; } = 0.05f;
		public virtual float X { get; set; } = 0f;
		public virtual float Y { get; set; } = 0.3f;
		public virtual float Z { get; set; } = 0f;
		public virtual float Angle { get; set; } = 45f;
		public virtual float TDAngle { get; set; } = 10f;
		public virtual bool Color { get; set; } = false;
		[UseConverter(typeof(ColorConverter))]
		public virtual Color Left { get; set; } = new Color(1f, 1f, 1f, 1f);
		[UseConverter(typeof(ColorConverter))]
		public virtual Color Right { get; set; } = new Color(1f, 1f, 1f, 1f);
		public virtual bool Ignore { get; set; } = true;
		public virtual bool Bloom { get; set; } = false;
		public virtual float Brightness { get; set; } = 0.75f;
		public virtual bool Rainbow { get; set; } = false;
		public virtual float Speed { get; set; } = 0.01f;

		/// <summary>
		/// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
		/// </summary>
		public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(Config other)
        {
            // This instance's members populated from other
        }
    }
}