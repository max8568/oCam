using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class CustomSizeTests
{
    [Fact]
    public void Typed_size_is_read_with_spaces_trimmed()
    {
        var (size, error) = CustomSize.Parse(" 直播 ", " 1600", "900 ");
        Assert.Null(error);
        Assert.Equal(new CustomSize("直播", 1600, 900), size);
    }

    [Fact]
    public void Full_width_digits_count_as_numbers() =>
        Assert.Equal(new CustomSize("", 1600, 900), CustomSize.Parse("", "１６００", "９００").Size);

    [Fact]
    public void Label_may_be_left_empty()
    {
        var (size, error) = CustomSize.Parse("", "800", "600");
        Assert.Null(error);
        Assert.Equal(new CustomSize("", 800, 600), size);
    }

    [Theory]
    [InlineData("", "600")]
    [InlineData("abc", "600")]
    [InlineData("800", "6 0 0")]
    [InlineData("800.5", "600")]
    public void Width_and_height_must_be_numbers(string width, string height)
    {
        var (size, error) = CustomSize.Parse("x", width, height);
        Assert.Null(size);
        Assert.Equal("寬和高要填數字", error);
    }

    [Theory]
    [InlineData(30, 600)]   // below the 32 minimum
    [InlineData(800, 8194)] // above the maximum
    [InlineData(-800, 600)]
    public void Width_and_height_must_be_in_range(int width, int height)
    {
        var (size, error) = CustomSize.Parse("x", width.ToString(), height.ToString());
        Assert.Null(size);
        Assert.Equal("寬和高要在 32 到 8192 之間", error);
    }

    [Fact]
    public void Odd_sides_are_allowed() =>
        Assert.Equal(new CustomSize("x", 801, 599), CustomSize.Parse("x", "801", "599").Size);

    [Fact]
    public void Smallest_and_largest_sides_are_allowed()
    {
        Assert.NotNull(CustomSize.Parse("", "32", "8192").Size);
        Assert.NotNull(CustomSize.Parse("", "8192", "32").Size);
    }

    [Fact]
    public void Label_is_at_most_20_characters()
    {
        Assert.NotNull(CustomSize.Parse(new string('字', 20), "800", "600").Size);
        var (size, error) = CustomSize.Parse(new string('字', 21), "800", "600");
        Assert.Null(size);
        Assert.Equal("標籤最多 20 個字", error);
    }

    [Fact]
    public void Label_length_counts_what_the_user_sees_as_characters()
    {
        Assert.NotNull(CustomSize.Parse(string.Concat(Enumerable.Repeat("🎮", 20)), "800", "600").Size);
        Assert.Null(CustomSize.Parse(string.Concat(Enumerable.Repeat("🎮", 21)), "800", "600").Size);
    }

    [Fact]
    public void Menu_text_shows_the_label_and_the_size()
    {
        Assert.Equal("直播（1600 × 900）", new CustomSize("直播", 1600, 900).MenuText);
        Assert.Equal("1600 × 900", new CustomSize("", 1600, 900).MenuText);
    }
}
