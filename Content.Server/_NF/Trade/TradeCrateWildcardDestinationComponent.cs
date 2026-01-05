namespace Content.Server._NF.Trade;

/// <summary>
/// This marks a station as matching all trade crate destinations.
/// Useful, for example, for black market stations.
/// </summary>
[RegisterComponent]
public sealed partial class TradeCrateWildcardDestinationComponent : Component;
