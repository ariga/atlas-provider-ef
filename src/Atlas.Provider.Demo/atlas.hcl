variable "dialect" {
  type = string
}

variable "context" {
  type    = string
  default = ""
}

locals {
  dev_url = {
    mysql = "docker://mysql/8/dev"
    postgres = "docker://postgres/15"
    sqlserver = "docker://sqlserver/2022-latest"
    sqlite = "sqlite://file::memory:?cache=shared"
  }[var.dialect]
  
  schema_url = var.context == "" ? data.external_schema.efcore.url : data.external_schema.efcore_context.url
}

data "external_schema" "efcore" {
  program = [
    "atlas-ef",
    "--", var.dialect,
  ]
}

data "external_schema" "efcore_context" {
  program = [
    "atlas-ef",
    "--context", var.context,
    "--", var.dialect,
  ]
}

env {
  name = atlas.env
  src = local.schema_url
  dev = local.dev_url
  migration {
    dir = var.context == "" ? "file://migrations/${var.dialect}" : "file://migrations/${var.dialect}/${var.context}"
  }
  format {
    migrate {
      diff = "{{ sql . \"  \" }}"
    }
  }
}