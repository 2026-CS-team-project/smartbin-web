namespace SmartBin.Models;

public record Alert(
    string BinId,
    string BinName,
    int FillLevel,
    int Threshold,
    DateTime Timestamp
);
