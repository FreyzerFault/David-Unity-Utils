# David Unity Utils

Paquete personal con mil utilidades para Unity que uso en todos mis proyectos.

Usa Git Dependency Resolver para añadir las dependencias de este paquete.

Unity no es compatible con dependencias dentro de dependencias. La manera tradicional es
que instales cada paquete tú a mano por cada proyecto en el que lo usas.

Pero para reducir la complejidad Git Dependency Resolver se encarga de añadir las dependencias
automáticamente. Solo te tienes que preocupar de añadirlo al proyecto siempre.

## Requisitos

### Dependencias

- [Git Dependency Resolver](https://github.com/mob-sakai/GitDependencyResolverForUnity.git)

Usa el Package Manager para añadirlas con la URL del repositorio.

O añádelo directamente en el "manifest.json":

```json
"dependencies": {
    "com.coffee.git-dependency-resolver": "https://github.com/mob-sakai/GitDependencyResolverForUnity.git"
}
```

### Scoped Registries

Añade los siguientes registros:

```json
{
  "name": "package.openupm.com",
  "url": "https://package.openupm.com",
  "scopes": ["com.azixmcaze.unityserializabledictionary"]
}
```

¿Cómo? 2 opciones:

- En Project Settings -> Package Manager -> Scoped Registries
- En el manifest.json

```json
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.azixmcaze.unityserializabledictionary"]
    }
  ],
```
