using System;
using System.Text;

public class LMKHeuristicWrapper
{
    public delegate bool Heuristic();

    public bool _heuristicResult = false;
    public bool _hasBeenTrue = false;

    public LMKHeuristicWrapper()
	{
	}

    public void Update(Heuristic heuristic)
    {
        _heuristicResult = heuristic();
        if (_heuristicResult)
        {
            _hasBeenTrue = true;
        }
    }

    public void AppendDebugText(string name, StringBuilder sb)
    {
        sb.AppendLine(string.Format(_hasBeenTrue ? "{0}*: {1}" : "{0}: {1}", name, _heuristicResult));
    }
}
