namespace MultiCamCapture.Demo
{
    /// <summary>
    /// Vertical depth zones / strata in the aquarium water column.
    /// </summary>
    public enum DepthZone
    {
        /// <summary>
        /// Roams freely across the entire water column.
        /// </summary>
        FullColumn = 0,

        /// <summary>
        /// Upper 25% of the water column near the surface.
        /// </summary>
        Surface = 1,

        /// <summary>
        /// Middle 50% open reef / mid-water area.
        /// </summary>
        MidWater = 2,

        /// <summary>
        /// Lower 25% benthic zone near the sand / substrate.
        /// </summary>
        Bottom = 3
    }
}
