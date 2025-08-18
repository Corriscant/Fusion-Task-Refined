using System;

namespace FusionTask.Gameplay
{
    /// <summary>
    /// Provides access to the selection state.
    /// </summary>
    public interface ISelectionService
    {
        /// <summary>
        /// Indicates whether a frame selection is currently active.
        /// </summary>
        bool IsSelecting { get; }

        /// <summary>
        /// Triggered when the selection state changes.
        /// The boolean parameter is <c>true</c> when selection starts and <c>false</c> when it ends.
        /// </summary>
        event Action<bool> SelectionStateChanged;
    }
}

