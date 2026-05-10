using System;

namespace FlyShadow.Audio.Strategies
{
    /// <summary>
    /// Defines behavior for selecting an audio cue from a collection of variations.
    /// </summary>
    public interface IPlaybackStrategy
    {
        /// <summary>
        /// Selects one of the provided variations for playback.
        /// </summary>
        /// <param name="variations">Collection of available variations.</param>
        /// <param name="state">Mutable state value maintained per binding.</param>
        /// <param name="random">Random generator for strategies that require randomness.</param>
        /// <returns>The selected audio cue, or null if selection is not possible.</returns>
        AudioCue SelectCue(AudioVariation[] variations, ref int state, Random random);
    }
}
