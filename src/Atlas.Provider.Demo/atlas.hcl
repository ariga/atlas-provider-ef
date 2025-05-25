
data "external_schema" "efcore" {
  program = [
    "dotnet",
    "atlas-ef",
    "--", "mysql",
  ]
}

env {
  name = atlas.env
  src = data.external_schema.efcore.url
  dev = "docker://mysql/8/dev"
  migration {
    dir = "file://migrations/mysql"
  }
  format {
    migrate {
      diff = "{{ sql . \"  \" }}"
    }
  }
}