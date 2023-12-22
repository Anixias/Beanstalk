namespace Beanstalk.Analysis.Text;

public interface IBuffer
{
	char this[int position] { get; }
	int Length { get; }
	string GetText();
	string GetText(TextRange range);
    (int, int) GetLineColumn(int position);
}