using TMPro;
using UnityEngine;

/// <summary>
/// TMP 글자 정점을 원본 기준으로 옮기고 늘리고 투명하게 만든다. 글자 낙하 연출에서 쓴다.
/// </summary>
public class LetterDropMesh
{
    private TMP_Text _text;
    private TMP_MeshInfo[] _source;
    private int[] _letters = System.Array.Empty<int>();

    /// <summary>
    /// 보이는 글자 수. 공백은 세지 않는다.
    /// </summary>
    public int LetterCount => _letters.Length;

    /// <summary>
    /// 현재 글자로 메시를 만들고 원본 정점과 색을 담는다.
    /// </summary>
    public void Capture(TMP_Text text)
    {
        _text = text;
        _source = null;
        _letters = System.Array.Empty<int>();
        if (_text == null)
        {
            return;
        }

        _text.ForceMeshUpdate();
        var textInfo = _text.textInfo;
        _source = textInfo.CopyMeshInfoVertexData();

        var count = 0;
        for (var i = 0; i < textInfo.characterCount; i++)
        {
            if (textInfo.characterInfo[i].isVisible)
            {
                count++;
            }
        }

        _letters = new int[count];
        var index = 0;
        for (var i = 0; i < textInfo.characterCount; i++)
        {
            if (textInfo.characterInfo[i].isVisible)
            {
                _letters[index++] = i;
            }
        }
    }

    /// <summary>
    /// letter번째 보이는 글자를 바닥 가운데 기준으로 늘린 뒤 offset만큼 옮기고 알파를 곱한다.
    /// </summary>
    public void Apply(int letter, Vector2 offset, Vector2 scale, float alpha)
    {
        if (_source == null || letter < 0 || letter >= _letters.Length)
        {
            return;
        }

        var textInfo = _text.textInfo;
        var character = textInfo.characterInfo[_letters[letter]];
        var materialIndex = character.materialReferenceIndex;
        if (materialIndex >= _source.Length || materialIndex >= textInfo.meshInfo.Length)
        {
            return;
        }

        var vertexIndex = character.vertexIndex;
        var sourceVertices = _source[materialIndex].vertices;
        var sourceColors = _source[materialIndex].colors32;
        var vertices = textInfo.meshInfo[materialIndex].vertices;
        var colors = textInfo.meshInfo[materialIndex].colors32;

        // TMP 정점 순서는 왼아래, 왼위, 오른위, 오른아래.
        var bottomLeft = sourceVertices[vertexIndex];
        var bottomRight = sourceVertices[vertexIndex + 3];
        var pivot = new Vector3((bottomLeft.x + bottomRight.x) * 0.5f, bottomLeft.y, 0f);
        var move = new Vector3(offset.x, offset.y, 0f);
        for (var i = 0; i < 4; i++)
        {
            var local = sourceVertices[vertexIndex + i] - pivot;
            vertices[vertexIndex + i] = pivot + new Vector3(local.x * scale.x, local.y * scale.y, local.z) + move;

            var color = sourceColors[vertexIndex + i];
            color.a = (byte)Mathf.RoundToInt(color.a * Mathf.Clamp01(alpha));
            colors[vertexIndex + i] = color;
        }
    }

    /// <summary>
    /// 바꾼 정점과 색을 메시에 올린다.
    /// </summary>
    public void Push()
    {
        if (_text == null || _source == null)
        {
            return;
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary>
    /// 모든 글자를 원본 자리와 색으로 되돌린다.
    /// </summary>
    public void Restore()
    {
        for (var i = 0; i < _letters.Length; i++)
        {
            Apply(i, Vector2.zero, Vector2.one, 1f);
        }

        Push();
    }
}
