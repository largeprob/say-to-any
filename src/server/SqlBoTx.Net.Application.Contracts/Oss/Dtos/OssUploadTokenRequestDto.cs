using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SqlBoTx.Net.Application.Contracts.Oss.Dtos;

public class OssUploadTokenRequestDto
{
    [DisplayName("文件名")]
    [Required(ErrorMessage = "{0}不能为空")]
    public string? FileName { get; set; }

    [DisplayName("文件类型")]
    [Required(ErrorMessage = "{0}不能为空")]
    public string? ContentType { get; set; }

    [DisplayName("文件大小")]
    [Required(ErrorMessage = "{0}不能为空")]
    public long? FileSize { get; set; }
}
