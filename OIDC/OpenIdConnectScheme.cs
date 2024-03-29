using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OIDC;

public class OpenIdConnectScheme
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Authority { get; set; } // URL нашего identity-провайдера

    public string ClientId { get; set; } // ClientId, созданный в idp

    public string ClientSecret { get; set; } // ClientSecret, созданный в idp
}