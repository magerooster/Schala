/*
 * Copyright (C) Rei HOBARA 2007
 * 
 * Name:
 *     RandomBase.cs
 * Class:
 *     Rei.Random.RandomBase
 * Purpose:
 *     A base class for random number generator.
 * Remark:
 * History:
 *     2007/10/6 initial release.
 * 
 */

using System;

namespace Rei.Random
{

    /// <summary>
    /// ŠeŽí‹[Ž——”ƒWƒFƒlƒŒ[ƒ^[—pŠî’êƒNƒ‰ƒXB
    /// ”h¶ƒNƒ‰ƒX‚ÍNextUInt32‚ðŽÀ‘•‚·‚é•K—v‚ª‚ ‚è‚Ü‚·B
    /// </summary>
    public abstract class RandomBase
    {

        /// <summary>
        /// ”h¶ƒNƒ‰ƒX‚Å•„†‚È‚µ32bit‚Ì‹[Ž——”‚ð¶¬‚·‚é•K—v‚ª‚ ‚è‚Ü‚·B
        /// </summary>
        public abstract UInt32 NextUInt32();

        /// <summary>
        /// •„†‚ ‚è32bit‚Ì‹[Ž——”‚ðŽæ“¾‚µ‚Ü‚·B
        /// </summary>
        public virtual Int32 NextInt32()
        {
            return (Int32)NextUInt32();
        }

        /// <summary>
        /// •„†‚È‚µ64bit‚Ì‹[Ž——”‚ðŽæ“¾‚µ‚Ü‚·B
        /// </summary>
        public virtual UInt64 NextUInt64()
        {
            return ((UInt64)NextUInt32() << 32) | NextUInt32();
        }

        /// <summary>
        /// •„†‚ ‚è64bit‚Ì‹[Ž——”‚ðŽæ“¾‚µ‚Ü‚·B
        /// </summary>
        public virtual Int64 NextInt64()
        {
            return ((Int64)NextUInt32() << 32) | NextUInt32();
        }

        /// <summary>
        /// ‹[Ž——”—ñ‚ð¶¬‚µAƒoƒCƒg”z—ñ‚É‡‚ÉŠi”[‚µ‚Ü‚·B
        /// </summary>
        public virtual void NextBytes(byte[] buffer)
        {
            int i = 0;
            UInt32 r;
            while (i + 4 <= buffer.Length)
            {
                r = NextUInt32();
                buffer[i++] = (byte)r;
                buffer[i++] = (byte)(r >> 8);
                buffer[i++] = (byte)(r >> 16);
                buffer[i++] = (byte)(r >> 24);
            }
            if (i >= buffer.Length) return;
            r = NextUInt32();
            buffer[i++] = (byte)r;
            if (i >= buffer.Length) return;
            buffer[i++] = (byte)(r >> 8);
            if (i >= buffer.Length) return;
            buffer[i++] = (byte)(r >> 16);
        }

        /// <summary>
        /// [0,1)‚Ì‹[Ž——”‚ðŽæ“¾‚µ‚Ü‚·B
        /// [0,1)‚ð2^53ŒÂ‚É‹Ï“™‚É‚í‚¯A‚»‚Ì‚¤‚¿ˆê‚Â‚ð•Ô‚µ‚Ü‚·B
        /// NextUInt32‚ð2‰ñŒÄ‚Ño‚µ‚Ü‚·B
        /// </summary>
        public virtual double NextDouble()
        {
            UInt32 r1, r2;
            r1 = NextUInt32();
            r2 = NextUInt32();
            return (r1 * (double)(2 << 11) + r2) / (double)(2 << 53);
        }

    }

}