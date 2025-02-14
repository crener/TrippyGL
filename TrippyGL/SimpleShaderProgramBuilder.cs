﻿using System;
using System.Text;

namespace TrippyGL
{
    /// <summary>
    /// Used to construct <see cref="SimpleShaderProgram"/> instances with the desired parameters.
    /// </summary>
    public struct SimpleShaderProgramBuilder : IEquatable<SimpleShaderProgramBuilder>
    {
        private static readonly object stringBuilderReferenceLock = new object();
        private static WeakReference<StringBuilder> stringBuilderReference;

        /// <summary>
        /// The version-profile string for GLSL to use. If null, the current GL version will be used.
        /// If set to an empty string, no version string will be used.
        /// <para>Don't include the "#version" in your string, write only the number and profile: "330 core"</para>
        /// </summary>
        public string GLSLVersionString;

        /// <summary>The index of the vertex attribute from which the <see cref="SimpleShaderProgram"/> will read vertex positions.</summary>
        public int PositionAttributeIndex;
        /// <summary>The index of the vertex attribute from which the <see cref="SimpleShaderProgram"/> will read vertex normals.</summary>
        public int NormalAttributeIndex;
        /// <summary>The index of the vertex attribute from which the <see cref="SimpleShaderProgram"/> will read vertex colors.</summary>
        public int ColorAttributeIndex;
        /// <summary>The index of the vertex attribute from which the <see cref="SimpleShaderProgram"/> will read vertex texcoords.</summary>
        public int TexCoordsAttributeIndex;

        /// <summary>Whether the <see cref="SimpleShaderProgram"/> will include vertex colors in the fragment calculation.</summary>
        public bool VertexColorsEnabled;
        /// <summary>
        /// Whether the <see cref="SimpleShaderProgram"/> will include vertex texcoords
        /// and sampling from a <see cref="Texture2D"/> in the fragment calculation.
        /// </summary>
        public bool TextureEnabled;

        /// <summary>The amount of directional lights to include in the <see cref="SimpleShaderProgram"/>. Zero to disable.</summary>
        public int DirectionalLights;
        /// <summary>The amount of positional lights to include in the <see cref="SimpleShaderProgram"/>. Zero to disable.</summary>
        public int PositionalLights;

        /// <summary>Whether to exclude the World matrix from the vertex shader.</summary>
        public bool ExcludeWorldMatrix;

        /// <summary>Whether to discard fragments considered transparent according to <see cref="TransparentFragmentThreshold"/>.</summary>
        /// <remarks>Discarding the fragment occurs before lighting calculations.</remarks>
        public bool DiscardTransparentFragments;

        /// <summary>The minimum amount of alpha required for the fragment to not be discarded.</summary>
        /// <remarks>
        /// This property is ignored unless <see cref="DiscardTransparentFragments"/> is set to true.
        /// If a fragment has exactly this alpha value, it will NOT be discarded.
        /// </remarks>
        public float TransparentFragmentThreshold;

        /// <summary>
        /// Gets whether the current configuration of this <see cref="SimpleShaderProgramBuilder"/>
        /// will make a created <see cref="SimpleShaderProgram"/> include lighting calculations.
        /// </summary>
        public bool LightingEnabled => DirectionalLights > 0 || PositionalLights > 0;

        /// <summary>The vertex shader log of the last <see cref="SimpleShaderProgram"/> this builder created.</summary>
        public string VertexShaderLog;
        /// <summary>The fragment shader log of the last <see cref="SimpleShaderProgram"/> this builder created.</summary>
        public string FragmentShaderLog;
        /// <summary>The program log of the last <see cref="SimpleShaderProgram"/> this builder created.</summary>
        public string ProgramLog;

        public static bool operator ==(SimpleShaderProgramBuilder left, SimpleShaderProgramBuilder right) => left.Equals(right);
        public static bool operator !=(SimpleShaderProgramBuilder left, SimpleShaderProgramBuilder right) => !left.Equals(right);

        /// <summary>
        /// Automatically sets the configuration of attribute indices.
        /// </summary>
        /// <remarks>
        /// This function loops through a vertex attributes descriptions. The first attribute found
        /// whose <see cref="VertexAttribDescription.AttribType"/> is <see cref="AttributeType.FloatVec3"/>
        /// will be used for Position, and the second will be used for Normal.
        /// The first ocurrance of a <see cref="AttributeType.FloatVec4"/> will be used for Color, and 
        /// the first ocurrance of a <see cref="AttributeType.FloatVec2"/> will be used for TexCoords.
        /// Any attribute left unassigned is set to -1.
        /// </remarks>
        /// <typeparam name="T">The type of vertex the <see cref="SimpleShaderProgram"/> will use.</typeparam>
        public void ConfigureVertexAttribs<T>() where T : unmanaged, IVertex
        {
            // We get the attrib descriptions from the vertex.
            T tmp = default;
            int attribDescCount = tmp.AttribDescriptionCount;
            Span<VertexAttribDescription> attribDescriptions = attribDescCount > 32 ?
                new VertexAttribDescription[attribDescCount] : stackalloc VertexAttribDescription[attribDescCount];
            tmp.WriteAttribDescriptions(attribDescriptions);

            // We set all the indices to -1 so we can assign them from scratch.
            PositionAttributeIndex = -1;
            NormalAttributeIndex = -1;
            ColorAttributeIndex = -1;
            TexCoordsAttributeIndex = -1;

            // We loop through all the attrib descriptions.
            uint attribIndex = 0;
            for (int i = 0; i < attribDescCount; i++)
            {
                // We skip padding descriptors because those don't affect the attrib indices.
                if (attribDescriptions[i].IsPadding)
                    continue;

                // We check which type of attrib this is and if necessary, set it in the right variable.
                if (attribDescriptions[i].AttribType == AttributeType.FloatVec3)
                {
                    if (PositionAttributeIndex == -1)
                        PositionAttributeIndex = (int)attribIndex;
                    else if (NormalAttributeIndex == -1)
                        NormalAttributeIndex = (int)attribIndex;
                }
                else if (attribDescriptions[i].AttribType == AttributeType.FloatVec4)
                {
                    if (ColorAttributeIndex == -1)
                        ColorAttributeIndex = (int)attribIndex;
                }
                else if (attribDescriptions[i].AttribType == AttributeType.FloatVec2)
                {
                    if (TexCoordsAttributeIndex == -1)
                        TexCoordsAttributeIndex = (int)attribIndex;
                }

                // We increment the attribIndex to advance the index to the next attrib.
                attribIndex += attribDescriptions[i].AttribIndicesUseCount;
            }

            // If no position was found, we throw an exception.
            if (PositionAttributeIndex == -1)
                throw new InvalidOperationException("Invalid vertex format. Must have at least a FloatVec3 for position.");
        }

        /// <summary>
        /// Creates a <see cref="SimpleShaderProgram"/> using the current values on this
        /// <see cref="SimpleShaderProgramBuilder"/>.
        /// </summary>
        /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> the <see cref="SimpleShaderProgram"/> will use.</param>
        public SimpleShaderProgram Create(GraphicsDevice graphicsDevice)
        {
            const string DifferentVertexAttribIndicesError = "All specified vertex attribute indices must be different.";

            VertexShaderLog = null;
            FragmentShaderLog = null;
            ProgramLog = null;

            if (graphicsDevice == null)
                throw new ArgumentNullException(nameof(graphicsDevice));

            if (PositionAttributeIndex < 0)
                throw new InvalidOperationException("A vertex attribute index for Position must always be specified.");

            if (DiscardTransparentFragments && !float.IsFinite(TransparentFragmentThreshold))
                throw new InvalidOperationException(nameof(TransparentFragmentThreshold) + " must be a finite value.");

            if (LightingEnabled)
            {
                if (NormalAttributeIndex < 0)
                    throw new InvalidOperationException("Using lighting requires a vertex Normal attribute.");
                if (NormalAttributeIndex == PositionAttributeIndex)
                    throw new InvalidOperationException(DifferentVertexAttribIndicesError);
            }

            if (VertexColorsEnabled)
            {
                if (ColorAttributeIndex < 0)
                    throw new InvalidOperationException("Using vertex colors requires a vertex Color attribute.");
                if (ColorAttributeIndex == PositionAttributeIndex || ColorAttributeIndex == NormalAttributeIndex)
                    throw new InvalidOperationException(DifferentVertexAttribIndicesError);
            }

            if (TextureEnabled)
            {
                if (TexCoordsAttributeIndex < 0)
                    throw new InvalidOperationException("Using textures requires a vertex TexCoords attribute.");
                if (TexCoordsAttributeIndex == PositionAttributeIndex || TexCoordsAttributeIndex == NormalAttributeIndex
                    || TexCoordsAttributeIndex == ColorAttributeIndex)
                    throw new InvalidOperationException(DifferentVertexAttribIndicesError);
            }

            StringBuilder builder = GetStringBuilder();
            ShaderProgramBuilder programBuilder = new ShaderProgramBuilder();
            uint programHandle = 0;

            try
            {
                bool useLighting = LightingEnabled;

                AppendGLSLVersionString(graphicsDevice, builder);
                builder.Append("\n\n");

                if (!ExcludeWorldMatrix)
                    builder.Append("uniform mat4 World;\n");
                builder.Append("uniform mat4 View, Projection;\n");

                builder.Append("\nin vec3 vPosition;\n");
                if (useLighting) builder.Append("in vec3 vNormal;\n");
                if (VertexColorsEnabled) builder.Append("in vec4 vColor;\n");
                if (TextureEnabled) builder.Append("in vec2 vTexCoords;\n");
                builder.Append('\n');
                if (useLighting) builder.Append("out vec3 fPosition;\nout vec3 fNormal;\n");
                if (VertexColorsEnabled) builder.Append("out vec4 fColor;\n");
                if (TextureEnabled) builder.Append("out vec2 fTexCoords;\n");

                builder.Append("\nvoid main() {\n");
                if (ExcludeWorldMatrix)
                    builder.Append("vec4 worldPos = vec4(vPosition, 1.0);\n");
                else
                    builder.Append("vec4 worldPos = World * vec4(vPosition, 1.0);\n");
                builder.Append("gl_Position = Projection * View * worldPos;\n\n");
                if (useLighting)
                {
                    builder.Append("fPosition = worldPos.xyz;\n");
                    if (ExcludeWorldMatrix)
                        builder.Append("fNormal = vNormal;\n");
                    else
                        builder.Append("fNormal = (World * vec4(vNormal, 0.0)).xyz;\n");
                }
                if (VertexColorsEnabled) builder.Append("fColor = vColor;\n");
                if (TextureEnabled) builder.Append("fTexCoords = vTexCoords;\n");

                builder.Append('}');

                programBuilder.VertexShaderCode = builder.ToString();
                builder.Clear();





                AppendGLSLVersionString(graphicsDevice, builder);
                builder.Append("\n\n");

                builder.Append("uniform vec4 Color;\n\n");

                if (TextureEnabled) builder.Append("uniform sampler2D samp;\n\n");

                if (useLighting)
                {
                    builder.Append("uniform vec3 cameraPos;\n");
                    builder.Append("uniform vec3 ambientLightColor;\n");
                    builder.Append("uniform float reflectivity;\n");
                    builder.Append("uniform float specularPower;\n");
                    for (int i = 0; i < DirectionalLights; i++)
                    {
                        string itostring = i.ToString();
                        builder.Append("uniform vec3 dLightDir");
                        builder.Append(itostring);
                        builder.Append(";\nuniform vec3 dLightDiffColor");
                        builder.Append(itostring);
                        builder.Append(";\nuniform vec3 dLightSpecColor");
                        builder.Append(itostring);
                        builder.Append(";\n");
                    }

                    for (int i = 0; i < PositionalLights; i++)
                    {
                        string itostring = i.ToString();
                        builder.Append("uniform vec3 pLightPos");
                        builder.Append(itostring);
                        builder.Append(";\nuniform vec3 pLightDiffColor");
                        builder.Append(itostring);
                        builder.Append(";\nuniform vec3 pLightSpecColor");
                        builder.Append(itostring);
                        builder.Append(";\nuniform vec3 pAttConfig");
                        builder.Append(itostring);
                        builder.Append(";\n");
                    }
                }

                if (useLighting) builder.Append("\nin vec3 fPosition;\nin vec3 fNormal;\n");
                if (VertexColorsEnabled) builder.Append("in vec4 fColor;\n");
                if (TextureEnabled) builder.Append("in vec2 fTexCoords;\n");
                builder.Append("\nout vec4 FragColor;\n");

                if (useLighting)
                {
                    builder.Append("\nvec3 calcDirLight(in vec3 norm, in vec3 toCamVec, in vec3 lDir, in vec3 diffCol, in vec3 specCol) {\n");
                    builder.Append("float brightness = max(0.0, dot(norm, -lDir));\n");
                    builder.Append("vec3 reflectedDir = reflect(lDir, norm);\n");
                    builder.Append("float specFactor = max(0.0, dot(reflectedDir, toCamVec));\n");
                    builder.Append("float dampedFactor = pow(specFactor, specularPower);\n");
                    builder.Append("return brightness * diffCol + (dampedFactor * reflectivity) * specCol;\n}\n");

                    if (PositionalLights > 0)
                    {
                        builder.Append("\nvec3 calcPosLight(in vec3 norm, in vec3 toCamVec, in vec3 lPos, in vec3 diffCol, in vec3 specCol, in vec3 attConfig) {\n");
                        builder.Append("float d = distance(fPosition, lPos);\n");
                        builder.Append("return calcDirLight(norm, toCamVec, normalize(fPosition - lPos), diffCol, specCol) * clamp(1.0 / (attConfig.x + attConfig.y*d + attConfig.z*d*d), 0.0, 1.0);\n");
                        builder.Append("}\n");
                    }
                }

                builder.Append("\nvoid main() {\n");
                builder.Append("vec4 finalColor = Color;\n");
                if (VertexColorsEnabled) builder.Append("finalColor *= fColor;\n");
                if (TextureEnabled) builder.Append("finalColor *= texture(samp, fTexCoords);\n");

                if (DiscardTransparentFragments)
                {
                    builder.Append("if (finalColor.A < ");
                    builder.Append(TransparentFragmentThreshold);
                    builder.Append(")\ndiscard;\n");
                }

                if (useLighting)
                {
                    builder.Append("vec3 unitNormal = normalize(fNormal);\n");
                    builder.Append("vec3 unitToCameraVec = normalize(cameraPos - fPosition);\n\n");
                    builder.Append("vec3 light = ambientLightColor;\n");

                    for (int i = 0; i < DirectionalLights; i++)
                    {
                        string itostring = i.ToString();
                        builder.Append("light += calcDirLight(unitNormal, unitToCameraVec, dLightDir");
                        builder.Append(itostring);
                        builder.Append(", dLightDiffColor");
                        builder.Append(itostring);
                        builder.Append(", dLightSpecColor");
                        builder.Append(itostring);
                        builder.Append(");\n");
                    }

                    for (int i = 0; i < PositionalLights; i++)
                    {
                        string itostring = i.ToString();
                        builder.Append("light += calcPosLight(unitNormal, unitToCameraVec, pLightPos");
                        builder.Append(itostring);
                        builder.Append(", pLightDiffColor");
                        builder.Append(itostring);
                        builder.Append(", pLightSpecColor");
                        builder.Append(itostring);
                        builder.Append(", pAttConfig");
                        builder.Append(itostring);
                        builder.Append(");\n");
                    }

                    builder.Append("finalColor.xyz *= light;\n");
                }

                builder.Append("FragColor = finalColor;\n}");

                programBuilder.FragmentShaderCode = builder.ToString();

                int maxAttribs = Math.Max(PositionAttributeIndex, Math.Max(NormalAttributeIndex, Math.Max(ColorAttributeIndex, TexCoordsAttributeIndex)));
                SpecifiedShaderAttrib[] attribs = new SpecifiedShaderAttrib[maxAttribs + 1];
                for (int i = 0; i < attribs.Length; i++)
                    attribs[i] = new SpecifiedShaderAttrib(null, AttributeType.Float);
                attribs[PositionAttributeIndex] = new SpecifiedShaderAttrib("vPosition", AttributeType.FloatVec3);
                if (NormalAttributeIndex >= 0)
                    attribs[NormalAttributeIndex] = new SpecifiedShaderAttrib(useLighting ? "vNormal" : null, AttributeType.FloatVec3);
                if (ColorAttributeIndex >= 0)
                    attribs[ColorAttributeIndex] = new SpecifiedShaderAttrib(VertexColorsEnabled ? "vColor" : null, AttributeType.FloatVec4);
                if (TexCoordsAttributeIndex >= 0)
                    attribs[TexCoordsAttributeIndex] = new SpecifiedShaderAttrib(TextureEnabled ? "vTexCoords" : null, AttributeType.FloatVec2);
                programBuilder.SpecifyVertexAttribs(attribs);

                // TODO: Change the getLogs parameter to false
                // Also, once we know the SimpleShaderProgram's code is flawless we should change this
                // somehow so it doesn't raise the GraphicsDevice.ShaderCompiled event.
                programHandle = programBuilder.CreateInternal(graphicsDevice, out ActiveVertexAttrib[] activeAttribs,
                    out bool hasVs, out bool hasGs, out bool hasFs, true);
                return new SimpleShaderProgram(graphicsDevice, programHandle, activeAttribs, hasVs, hasGs, hasFs,
                    VertexColorsEnabled, TextureEnabled, DirectionalLights, PositionalLights);
            }
            catch
            {
                // If anything goes wrong, we delete the program handle (since we're not returning a
                // ShaderProgram so it'd get lost into oblivion otherwise) and re-throw the exception.
                graphicsDevice.GL.DeleteProgram(programHandle);
                throw;
            }
            finally
            {
                VertexShaderLog = programBuilder.VertexShaderLog;
                FragmentShaderLog = programBuilder.FragmentShaderLog;
                ProgramLog = programBuilder.ProgramLog;

                builder.Clear();
                ReturnStringBuilder(builder);
            }
        }

        private void AppendGLSLVersionString(GraphicsDevice graphicsDevice, StringBuilder stringBuilder)
        {
            if (GLSLVersionString != null)
            {
                if (!string.IsNullOrWhiteSpace(GLSLVersionString))
                {
                    stringBuilder.Append("#version ");
                    stringBuilder.Append(GLSLVersionString);
                }
            }
            else
            {
                stringBuilder.Append("#version ");

                if (graphicsDevice.IsGLVersionAtLeast(3, 3))
                {
                    stringBuilder.Append((char)(graphicsDevice.GLMajorVersion + '0'));
                    stringBuilder.Append((char)(graphicsDevice.GLMinorVersion + '0'));
                }
                else
                {
                    stringBuilder.Append('1');
                    stringBuilder.Append((char)(graphicsDevice.GLMinorVersion + '3'));
                }

                stringBuilder.Append('0');
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = GLSLVersionString == null ? 0 : GLSLVersionString.GetHashCode(StringComparison.Ordinal);
                hashCode = (hashCode * 397) ^ PositionAttributeIndex;
                hashCode = (hashCode * 397) ^ NormalAttributeIndex;
                hashCode = (hashCode * 397) ^ ColorAttributeIndex;
                hashCode = (hashCode * 397) ^ TexCoordsAttributeIndex;
                hashCode = (hashCode * 397) ^ VertexColorsEnabled.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureEnabled.GetHashCode();
                hashCode = (hashCode * 397) ^ DirectionalLights;
                hashCode = (hashCode * 397) ^ PositionalLights;
                return hashCode;
            }
        }

        public bool Equals(SimpleShaderProgramBuilder other)
        {
            return ReferenceEquals(GLSLVersionString, other.GLSLVersionString)
                && PositionAttributeIndex == other.PositionAttributeIndex
                && NormalAttributeIndex == other.NormalAttributeIndex
                && ColorAttributeIndex == other.ColorAttributeIndex
                && TexCoordsAttributeIndex == other.TexCoordsAttributeIndex
                && VertexColorsEnabled == other.VertexColorsEnabled
                && TextureEnabled == other.TextureEnabled
                && DirectionalLights == other.DirectionalLights
                && PositionalLights == other.PositionalLights;
        }

        public override bool Equals(object obj)
        {
            if (obj is SimpleShaderProgramBuilder other)
                return Equals(other);
            return false;
        }

        private static StringBuilder GetStringBuilder()
        {
            lock (stringBuilderReferenceLock)
            {
                if (stringBuilderReference != null && stringBuilderReference.TryGetTarget(out StringBuilder builder))
                {
                    stringBuilderReference.SetTarget(null);
                    builder.Clear();
                    return builder;
                }

                return new StringBuilder(1024);
            }
        }

        private static void ReturnStringBuilder(StringBuilder stringBuilder)
        {
            lock (stringBuilderReferenceLock)
            {
                if (stringBuilderReference == null)
                    stringBuilderReference = new WeakReference<StringBuilder>(stringBuilder);
                else
                {
                    if (!stringBuilderReference.TryGetTarget(out StringBuilder oldBuilder) || oldBuilder == null || oldBuilder.Length < stringBuilder.Length)
                        stringBuilderReference.SetTarget(stringBuilder);
                }
            }
        }
    }
}
