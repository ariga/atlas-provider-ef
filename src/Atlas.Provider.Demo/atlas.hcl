variable "dialect" {
  type = string
}

locals {
  dev_url = {
    mysql = "docker://mysql/8/dev"
    postgres = "docker://postgres/15"
    sqlserver = "docker://sqlserver/2022-latest"
    sqlite = "sqlite://file::memory:?cache=shared"
  }[var.dialect]
}

data "external_schema" "ef" {
  program = [
    "dotnet",
    "atlas-ef",
    "--", var.dialect,
  ]
}

env "ef" {
  src = data.external_schema.ef.url
  dev = local.dev_url
  migration {
    dir = "file://migrations/${var.dialect}"
  }
  format {
    migrate {
      diff = "{{ sql . \"  \" }}"
    }
  }
}