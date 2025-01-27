using UnityEngine;

namespace Compendium.Extensions.RichText;

public static class RichTextExtensions
{
	public static string WrapWithTag(this string text, string tag)
	{
		return "<" + tag + ">" + text + "</" + tag + ">";
	}

	public static string WrapWithTag(this string text, string tag, string value)
	{
		if (value != null)
		{
			return "<" + tag + "=" + value + ">" + text + "</" + tag + ">";
		}
		return text.WrapWithTag(tag);
	}

	public static string Bold(this string text)
	{
		return text.WrapWithTag("b");
	}

	public static string Italic(this string text)
	{
		return text.WrapWithTag("i");
	}

	public static string Underline(this string text)
	{
		return text.WrapWithTag("u");
	}

	public static string Strikethrough(this string text)
	{
		return text.WrapWithTag("s");
	}

	public static string Superscript(this string text)
	{
		return text.WrapWithTag("sup");
	}

	public static string Subscript(this string text)
	{
		return text.WrapWithTag("sub");
	}

	public static string Color(this string text, string color)
	{
		return text.WrapWithTag("color", color);
	}

	public static string Color(this string text, Color color, bool alpha = false)
	{
		return text.Color(color.ToHex(alpha));
	}

	public static string Size(this string text, int size)
	{
		return text.Size($"{size}px");
	}

	public static string Size(this string text, string size)
	{
		return text.WrapWithTag("size", size);
	}

	public static string Align(this string text, RichTextAlignment alignment)
	{
		return text.WrapWithTag(alignment.ToString().ToLower());
	}

	public static string Mark(this string text, string color)
	{
		return text.WrapWithTag("mark", color);
	}

	public static string Mark(this string text, Color color)
	{
		return text.Mark(color.ToHex());
	}

	public static string Mark(this string text, Color color, byte alpha)
	{
		return text.Mark(color.ToHex(includeHash: true, includeAlpha: false) + alpha.ToString("X2"));
	}

	public static string NoParse(this string text)
	{
		return text.WrapWithTag("noparse");
	}

	public static string Capitalize(this string text, RichTextCapitalization mode)
	{
		return text.WrapWithTag(mode.ToString().ToLower());
	}

	public static string CharacterSpacing(this string text, int spacing)
	{
		return text.CharacterSpacing($"{spacing}px");
	}

	public static string CharacterSpacing(this string text, string spacing)
	{
		return text.WrapWithTag("cspace", spacing);
	}

	public static string Indent(this string text, int amount)
	{
		return text.Indent($"{amount}px");
	}

	public static string Indent(this string text, string amount)
	{
		return text.WrapWithTag("indent", amount);
	}

	public static string LineHeight(this string text, int spacing)
	{
		return text.LineHeight($"{spacing}px");
	}

	public static string LineHeight(this string text, string spacing)
	{
		return "<line-height=" + spacing + ">" + text;
	}

	public static string LineIndent(this string text, int amount)
	{
		return text.LineIndent($"{amount}px");
	}

	public static string LineIndent(this string text, string amount)
	{
		return "<line-indent=" + amount + ">" + text;
	}

	public static string Link(this string text, string id)
	{
		return text.WrapWithTag("link", "\"" + id + "\"");
	}

	public static string HorizontalPosition(this string text, int offset)
	{
		return text.HorizontalPosition($"{offset}px");
	}

	public static string HorizontalPosition(this string text, string offset)
	{
		return "<pos=" + offset + ">" + text;
	}

	public static string Margin(this string text, int margin, RichTextAlignment alignment = RichTextAlignment.Center)
	{
		return text.Margin($"{margin}px", alignment);
	}

	public static string Margin(this string text, string margin, RichTextAlignment alignment = RichTextAlignment.Center)
	{
		return "<margin" + ((alignment == RichTextAlignment.Center) ? "" : ("-" + alignment.ToString().ToLower())) + ">" + text;
	}

	public static string Monospace(this string text, float spacing = 1f)
	{
		return text.WrapWithTag("mspace", $"{spacing}em");
	}

	public static string Monospace(this string text, string spacing)
	{
		return text.WrapWithTag("mspace", spacing);
	}

	public static string VerticalOffset(this string text, int offset)
	{
		return text.VerticalOffset($"{offset}px");
	}

	public static string VerticalOffset(this string text, string offset)
	{
		return text.WrapWithTag("voffset", offset);
	}

	public static string MaxWidth(this string text, int width)
	{
		return text.MaxWidth($"{width}px");
	}

	public static string MaxWidth(this string text, string width)
	{
		return "<width=" + width + ">" + text;
	}

	public static string Sprite(int index, string color = null)
	{
		return string.Format("<sprite={0}{1}>", index, (color == null) ? "" : (" color=" + color));
	}

	public static string Sprite(int index, Color color)
	{
		return $"<sprite={index} color={color.ToHex()}>";
	}

	public static string Space(int amount)
	{
		return $"<space={amount}px>";
	}

	public static string Space(string amount)
	{
		return "<space=" + amount + ">";
	}
}
