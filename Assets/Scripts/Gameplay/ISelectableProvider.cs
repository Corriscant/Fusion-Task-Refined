namespace FusionTask.Gameplay
{
    /// <summary>
    /// Provides access to an <see cref="ISelectable"/> instance.
    /// </summary>
    public interface ISelectableProvider
    {
        /// <summary>
        /// Returns the selectable instance associated with the object.
        /// </summary>
        ISelectable Selectable { get; }
    }
}
