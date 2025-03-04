using System;
using System.Collections.ObjectModel;
using ReactiveUI;
using dxfInspect.Model;
using dxfInspect.Services;

namespace dxfInspect.ViewModels;

public class DxfTreeNodeViewModel : ReactiveObject
{
    private bool _isExpanded;
    private DxfLineRange _lineRange;
    private int _endLine;
    private long _dataSize;
    private long _totalDataSize;
    private string _data;
    private int _code;
    private string _originalGroupCodeLine;
    private string _originalDataLine;
    private int _objectCount;

    public DxfTreeNodeViewModel(
        int startLine,
        int endLine,
        int code,
        string data,
        string type,
        string nodeKey,
        string originalGroupCodeLine,
        string originalDataLine,
        DxfRawTag rawTag)
    {
        StartLine = startLine;
        _endLine = endLine;
        _lineRange = new DxfLineRange(startLine, endLine);
        _code = code;
        _data = data;
        Type = type;
        NodeKey = nodeKey;
        _originalGroupCodeLine = originalGroupCodeLine;
        _originalDataLine = originalDataLine;
        ExtractWhitespace(originalGroupCodeLine, originalDataLine);
        RawTag = rawTag;
        _dataSize = System.Text.Encoding.UTF8.GetByteCount(Data);
        _totalDataSize = _dataSize;

        UpdateDescription();
        UpdateValueType();
        UpdateObjectCount();
    }

    public DxfTreeNodeViewModel? Parent { get; set; }
    public int StartLine { get; }
    private string _groupCodeDescription = "";
    private string _groupCodeValueType = "";

    public int EndLine
    {
        get => _endLine;
        set => this.RaiseAndSetIfChanged(ref _endLine, value);
    }

    public DxfLineRange LineRange
    {
        get => _lineRange;
        private set => this.RaiseAndSetIfChanged(ref _lineRange, value);
    }

    public int Code
    {
        get => _code;
        set
        {
            this.RaiseAndSetIfChanged(ref _code, value);
            UpdateNodeKey();
            UpdateOriginalLines();
            UpdateDescription();
            UpdateValueType();
                    
            // Update the underlying raw tag
            if (RawTag != null)
            {
                RawTag.GroupCode = value;
            }
        }
    }

    public string Data
    {
        get => _data;
        set
        {
            this.RaiseAndSetIfChanged(ref _data, value);
            _dataSize = System.Text.Encoding.UTF8.GetByteCount(value);
            UpdateTotalDataSize();
            UpdateNodeKey();
            UpdateOriginalLines();
                    
            // Update the underlying raw tag
            if (RawTag != null)
            {
                RawTag.DataElement = value;
            }
        }
    }

    public string Type { get; }

    public string NodeKey { get; private set; }

    public string OriginalGroupCodeLine
    {
        get => _originalGroupCodeLine;
        private set => this.RaiseAndSetIfChanged(ref _originalGroupCodeLine, value);
    }

    public string OriginalDataLine
    {
        get => _originalDataLine;
        private set => this.RaiseAndSetIfChanged(ref _originalDataLine, value);
    }

    public DxfRawTag RawTag { get; }
        
    public ObservableCollection<DxfTreeNodeViewModel> Children { get; } = [];
        
    public bool HasChildren => Children.Count > 0;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public string CodeString => Code.ToString();

    public string GroupCodeDescription
    {
        get => _groupCodeDescription;
        private set => this.RaiseAndSetIfChanged(ref _groupCodeDescription, value);
    }

    public string GroupCodeValueType
    {
        get => _groupCodeValueType;
        private set => this.RaiseAndSetIfChanged(ref _groupCodeValueType, value);
    }
        
    public void UpdateObjectCount()
    {
        int count = 0;
        
        if (HasChildren)
        {
            foreach (var child in Children)
            {
                child.UpdateObjectCount();

                if (child.Code == DxfParser.DxfCodeForType)
                {
                    count++;
                }
            }
        }
        
        ObjectCount = count;
    }
        
    public void UpdateTotalDataSize()
    {
        UpdateObjectCount();
            
        var newTotal = _dataSize;
        if (HasChildren)
        {
            foreach (var child in Children)
            {
                child.UpdateTotalDataSize();
                newTotal += child._totalDataSize;
            }
        }

        this.RaiseAndSetIfChanged(ref _totalDataSize, newTotal, nameof(TotalDataSize));
        this.RaisePropertyChanged(nameof(FormattedDataSize));
    }

    public long TotalDataSize => _totalDataSize;

    public string FormattedDataSize
    {
        get
        {
            var size = _totalDataSize;
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double calculatedSize = size;

            while (calculatedSize >= 1024 && order < sizes.Length - 1)
            {
                order++;
                calculatedSize /= 1024;
            }

            return $"{calculatedSize:0.##} {sizes[order]}";
        }
    }
        
    public int ObjectCount
    {
        get => _objectCount;
        private set => this.RaiseAndSetIfChanged(ref _objectCount, value);
    }
        
    private void UpdateNodeKey()
    {
        string type = Code == DxfParser.DxfCodeForType ? Data : Code.ToString();
        NodeKey = $"{StartLine}:{type}:{Data}";
        this.RaisePropertyChanged(nameof(NodeKey));
    }

    private string _leadingCodeWhitespace = "";
    private string _trailingCodeWhitespace = "";
    private string _leadingDataWhitespace = "";
    private string _trailingDataWhitespace = "";

    private void ExtractWhitespace(string originalGroupCodeLine, string originalDataLine)
    {
        try 
        {
            // Handle group code line whitespace
            var groupCodeTrimmed = originalGroupCodeLine?.TrimStart() ?? "";
            var groupCodeTrimmedEnd = originalGroupCodeLine?.TrimEnd() ?? "";
        
            _leadingCodeWhitespace = originalGroupCodeLine?.Length > 0 && groupCodeTrimmed.Length < originalGroupCodeLine.Length 
                ? originalGroupCodeLine[..(originalGroupCodeLine.Length - groupCodeTrimmed.Length)]
                : "";
            
            _trailingCodeWhitespace = groupCodeTrimmedEnd.Length > 0 && groupCodeTrimmed.Length > Code.ToString().Length
                ? groupCodeTrimmed[Code.ToString().Length..]
                : "";

            // Handle data line whitespace
            var dataTrimmed = originalDataLine?.TrimStart() ?? "";
            var dataTrimmedEnd = originalDataLine?.TrimEnd() ?? "";
        
            _leadingDataWhitespace = originalDataLine?.Length > 0 && dataTrimmed.Length < originalDataLine.Length
                ? originalDataLine[..(originalDataLine.Length - dataTrimmed.Length)]
                : "";
            
            _trailingDataWhitespace = dataTrimmedEnd.Length > 0 && dataTrimmed.Length > Data.Length
                ? dataTrimmed[Data.Length..]
                : "";
        }
        catch
        {
            // If any error occurs during whitespace extraction, reset to empty strings
            _leadingCodeWhitespace = "";
            _trailingCodeWhitespace = "";
            _leadingDataWhitespace = "";
            _trailingDataWhitespace = "";
        }
    }

    private void UpdateOriginalLines()
    {
        OriginalGroupCodeLine = _leadingCodeWhitespace + Code.ToString() + _trailingCodeWhitespace;
        OriginalDataLine = _leadingDataWhitespace + Data + _trailingDataWhitespace;
    }

    private void UpdateDescription()
    {
        GroupCodeDescription = DxfGroupCodeInfo.GetDescription(Code);
    }

    private void UpdateValueType()
    {
        GroupCodeValueType = DxfGroupCodeInfo.GetValueType(Code);
    }

    public void UpdateLineRange(int startLine, int endLine)
    {
        _lineRange = new DxfLineRange(startLine, endLine);
        this.RaisePropertyChanged(nameof(LineRange));
    }
}
