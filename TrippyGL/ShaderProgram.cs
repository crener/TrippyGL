﻿using System;
using OpenTK.Graphics.OpenGL4;

namespace TrippyGL
{
    /// <summary>
    /// Encapsulates an OpenGL program object for using shaders.
    /// Shaders define how things are processed in the graphics card, from calculating vertex positions to choosing the color of each fragment
    /// </summary>
    public class ShaderProgram : GraphicsResource
    {
        /// <summary>The handle for the OpenGL Progrma object</summary>
        public readonly int Handle;

        private int vsHandle = -1;
        private int gsHandle = -1;
        private int fsHandle = -1;
        private bool areAttribsBound = false;

        /// <summary>Gets data about the geometry shader in this program, if there is one</summary>
        public GeometryShaderData GeometryShader { get; private set; }

        /// <summary>The list of uniforms in this program</summary>
        public ShaderUniformList Uniforms { get; private set; }

        /// <summary>The list of block uniforms in this program</summary>
        public ShaderBlockUniformList BlockUniforms { get; private set; }

        /// <summary>Whether this ShaderProgram has been linked</summary>
        public bool IsLinked { get; private set; } = false;

        /// <summary>Whether this ShaderProgram is the one currently in use</summary>
        public bool IsCurrentlyInUse { get { return GraphicsDevice.IsShaderProgramInUse(this); } }

        /// <summary>Whether this ShaderProgram has a vertex shader attached</summary>
        public bool HasVertexShader { get { return vsHandle != -1; } }

        /// <summary>Whether this ShaderProgram has a geometry shader attached</summary>
        public bool HasGeometryShader { get { return gsHandle != -1; } }

        /// <summary>Whether this ShaderProgram has a fragment shader attached</summary>
        public bool HasFragmentShader { get { return fsHandle != -1; } }

        /// <summary>
        /// Creates a ShaderProgram
        /// </summary>
        /// <param name="graphicsDevice">The GraphicsDevice this resource will use</param>
        public ShaderProgram(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
            Handle = GL.CreateProgram();
        }

        /// <summary>
        /// Adds a vertex shader to this ShaderProgram
        /// </summary>
        /// <param name="code">The GLSL code for the vertex shader</param>
        public void AddVertexShader(string code)
        {
            if (!TryAddVertexShader(code, out string log))
                throw new ShaderCompilationException(log);
        }

        /// <summary>
        /// Tries to add a vertex shader to the ShaderProgram. Returns whether it was successful
        /// </summary>
        /// <param name="code">The vertex shader's code</param>
        /// <param name="code">The GLSL code for the vertex shader</param>
        public bool TryAddVertexShader(string code, out string shaderLog)
        {
            ValidateUnlinked();

            if (String.IsNullOrEmpty(code))
                throw new ArgumentException("You must specify shader code", "code");

            if (vsHandle != -1)
                throw new InvalidOperationException("This ShaderProgram already has a vertex shader");

            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, code);
            GL.CompileShader(vs);
            GL.GetShader(vs, ShaderParameter.CompileStatus, out int status);
            shaderLog = GL.GetShaderInfoLog(vs);
            
            if (status == (int)All.False)
                return false;

            vsHandle = vs;
            GL.AttachShader(this.Handle, vsHandle);
            return true;
        }

        /// <summary>
        /// Tries to add a vertex shader to the ShaderProgram. Returns whether it was successful
        /// </summary>
        /// <param name="code">The GLSL code for the vertex shader</param>
        public bool TryAddVertexShader(string code)
        {
            ValidateUnlinked();

            if (String.IsNullOrEmpty(code))
                throw new ArgumentException("You must specify shader code", "code");

            if (vsHandle != -1)
                throw new InvalidOperationException("This ShaderProgram already has a vertex shader");

            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, code);
            GL.CompileShader(vs);
            GL.GetShader(vs, ShaderParameter.CompileStatus, out int status);

            if (status == (int)All.False)
                return false;

            vsHandle = vs;
            GL.AttachShader(this.Handle, vsHandle);
            return true;
        }

        /// <summary>
        /// Adds a geometry shader to this ShaderProgram
        /// </summary>
        /// <param name="code">The GLSL code for the geometry shader</param>
        public void AddGeometryShader(string code)
        {
            if (!TryAddGeometryShader(code, out string log))
                throw new ShaderCompilationException(log);
        }

        /// <summary>
        /// Tries to add a geometry shader to the ShaderProgram. Returns whether it was successful
        /// </summary>
        /// <param name="code">The GLSL code for the geometry shader</param>
        /// <param name="shaderLog">The compilation log from the shader</param>
        public bool TryAddGeometryShader(string code, out string shaderLog)
        {
            ValidateUnlinked();

            if (String.IsNullOrEmpty(code))
                throw new ArgumentException("You must specify shader code", "code");

            if (gsHandle != -1)
                throw new InvalidOperationException("This ShaderProgram already has a geometry shader");

            int gs = GL.CreateShader(ShaderType.GeometryShader);
            GL.ShaderSource(gs, code);
            GL.CompileShader(gs);
            GL.GetShader(gs, ShaderParameter.CompileStatus, out int status);
            shaderLog = GL.GetShaderInfoLog(gs);

            if (status == (int)All.False)
                return false;

            gsHandle = gs;
            GL.AttachShader(this.Handle, gsHandle);
            return true;
        }

        /// <summary>
        /// Tries to add a geometry shader to the ShaderProgram. Returns whether it was successful
        /// </summary>
        /// <param name="code">The GLSL code for the geometry shader</param>
        public bool TryAddGeometryShader(string code)
        {
            ValidateUnlinked();

            if (String.IsNullOrEmpty(code))
                throw new ArgumentException("You must specify shader code", "code");

            if (gsHandle != -1)
                throw new InvalidOperationException("This ShaderProgram already has a geometry shader");

            int gs = GL.CreateShader(ShaderType.GeometryShader);
            GL.ShaderSource(gs, code);
            GL.CompileShader(gs);
            GL.GetShader(gs, ShaderParameter.CompileStatus, out int status);

            if (status == (int)All.False)
                return false;

            gsHandle = gs;
            GL.AttachShader(this.Handle, gsHandle);
            return true;
        }

        /// <summary>
        /// Adds a fragment shader to this ShaderProgram
        /// </summary>
        /// <param name="code">The GLSL code for the fragment shader</param>
        public void AddFragmentShader(string code)
        {
            if (!TryAddFragmentShader(code, out string log))
                throw new ShaderCompilationException(log);
        }

        /// <summary>
        /// Tries to add a fragment shader to the ShaderProgram. Returns whether it was successful
        /// </summary>
        /// <param name="code">The GLSL code for the fragment shader</param>
        /// <param name="shaderLog">The compilation log from the shader</param>
        public bool TryAddFragmentShader(string code, out string shaderLog)
        {
            ValidateUnlinked();

            if (String.IsNullOrEmpty(code))
                throw new ArgumentException("You must specify shader code", "code");

            if (fsHandle != -1)
                throw new InvalidOperationException("This ShaderProgram already has a fragment shader");

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, code);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out int status);
            shaderLog = GL.GetShaderInfoLog(fs);

            if (status == (int)All.False)
                return false;

            fsHandle = fs;
            GL.AttachShader(this.Handle, fsHandle);
            return true;
        }

        /// <summary>
        /// Tries to add a fragment shader to the ShaderProgram. Returns whether it was successful
        /// </summary>
        /// <param name="code">The GLSL code for the fragment shader</param>
        public bool TryAddFragmentShader(string code)
        {
            ValidateUnlinked();

            if (String.IsNullOrEmpty(code))
                throw new ArgumentException("You must specify shader code", "code");

            if (fsHandle != -1)
                throw new InvalidOperationException("This ShaderProgram already has a fragment shader");

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, code);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out int status);

            if (status == (int)All.False)
                return false;

            fsHandle = fs;
            GL.AttachShader(this.Handle, fsHandle);
            return true;
        }

        /// <summary>
        /// Specifies the input vertex attributes for this ShaderProgram declared on the vertex shader
        /// </summary>
        /// <param name="attribData">The input attributes's descriptions, ordered by attribute index</param>
        /// <param name="attribNames">The input attribute's names, ordered by attribute index</param>
        public void SpecifyVertexAttribs(VertexAttribDescription[] attribData, string[] attribNames)
        {
            ValidateUnlinked();

            //if (vsHandle == -1) //this order is actually not a requirement on OpenGL...
            //    throw new InvalidOperationException("You must add a vertex shader before specifying vertex attributes");

            if (areAttribsBound)
                throw new InvalidOperationException("Attributes have already been bound for this program");

            if (attribData == null)
                throw new ArgumentNullException("attribData");

            if (attribNames == null)
                throw new ArgumentNullException("attribNames");

            if (attribData.Length == 0)
                throw new ArgumentException("There must be at least one attribute source", "attribData");

            if (attribData.Length != attribNames.Length)
                throw new ArgumentException("The attribData and attribNames arrays must have matching lengths");

            int index = 0;
            for(int i=0; i<attribNames.Length; i++)
            {
                if (String.IsNullOrEmpty(attribNames[i]))
                    throw new ArgumentException("All names in the array must have be valid");

                GL.BindAttribLocation(Handle, index, attribNames[i]);
                index += attribData[i].AttribIndicesUseCount;
            }

            areAttribsBound = true;
        }

        /// <summary>
        /// Specifies the input vertex attributes for this ShaderProgram declared on the vertex shader
        /// </summary>
        /// <param name="attribSources">The input attribute's descriptions, ordered by attribute index</param>
        /// <param name="attribNames">The input attribute's names, ordered by attribute index</param>
        public void SpecifyVertexAttribs(VertexAttribSource[] attribSources, string[] attribNames)
        {
            VertexAttribDescription[] attribData = new VertexAttribDescription[attribSources.Length];
            for (int i = 0; i < attribData.Length; i++)
                attribData[i] = attribSources[i].AttribDescription;
            SpecifyVertexAttribs(attribData, attribNames);
        }

        /// <summary>
        /// Specifies the input vertex attributes for this ShaderProgram declared on the vertex shader
        /// </summary>
        /// <typeparam name="T">The type of vertex this ShaderProgram will use as input</typeparam>
        /// <param name="attribNames">The input attribute's names, ordered by attribute index</param>
        public void SpecifyVertexAttribs<T>(string[] attribNames) where T : struct, IVertex
        {
            SpecifyVertexAttribs(new T().AttribDescriptions, attribNames);
        }

        /// <summary>
        /// Links the program.
        /// Once the program has been linked, it cannot be modifyed anymore, so make sure you add all your necessary shaders and specify vertex attributes
        /// </summary>
        public void LinkProgram()
        {
            ValidateUnlinked();

            if (vsHandle == -1)
                throw new InvalidOperationException("Shader program must have a vertex shader before linking!");
            
            if (!areAttribsBound)
                throw new InvalidOperationException("The vertex attributes's indices have never been specified");

            GL.LinkProgram(Handle);
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int status);
            if (status == (int)All.False)
                throw new ProgramLinkException(GL.GetProgramInfoLog(Handle));
            IsLinked = true;

            GL.DetachShader(Handle, vsHandle);
            GL.DeleteShader(vsHandle);

            if(fsHandle != -1)
            {
                GL.DetachShader(Handle, fsHandle);
                GL.DeleteShader(fsHandle);
            }

            if(gsHandle != -1)
            {
                GL.DetachShader(Handle, gsHandle);
                GL.DeleteShader(gsHandle);

                this.GeometryShader = new GeometryShaderData(this.Handle);
            }

            BlockUniforms = new ShaderBlockUniformList(this);
            Uniforms = new ShaderUniformList(this);
        }

        /// <summary>
        /// Ensures this program is the one currently in use for it's GraphicsDevice
        /// </summary>
        internal void EnsureInUse()
        {
            GraphicsDevice.EnsureShaderProgramInUse(this);
        }

        /// <summary>
        /// Ensures all necessary states are set for a draw command to use this program.
        /// This includes making sure sampler or block uniforms are properly set and the program is currently in use
        /// </summary>
        public void EnsurePreDrawStates()
        {
            GraphicsDevice.EnsureShaderProgramInUse(this);
            Uniforms.EnsureSamplerUniformsSet();
            BlockUniforms.EnsureAllSet();
        }

        /// <summary>
        /// Make sure this program is unlinked and throw a proper exception otherwise
        /// </summary>
        internal void ValidateUnlinked()
        {
            if (IsLinked)
                throw new InvalidOperationException("The program has already been linked");
        }

        /// <summary>
        /// Make sure this program is linked and throw a proper exception otherwise
        /// </summary>
        internal void ValidateLinked()
        {
            if (!IsLinked)
                throw new InvalidOperationException("The program must be linked first");
        }

        protected override void Dispose(bool isManualDispose)
        {
            GL.DeleteProgram(Handle);
            base.Dispose(isManualDispose);
        }


        /// <summary>
        /// Stores data about a geometry shader
        /// </summary>
        public class GeometryShaderData
        {
            /// <summary>The PrimitiveType the geometry shader takes as input</summary>
            public readonly PrimitiveType GeometryInputType;

            /// <summary>The PrimitiveType the geometry shader takes as output</summary>
            public readonly PrimitiveType GeometryOutputType;

            /// <summary>The amount of invocations the geometry shader will do</summary>
            public readonly int GeometryShaderInvocations;

            /// <summary>The maximum amount of vertices the geometry shader can output</summary>
            public readonly int GeometryVerticesOut;

            internal GeometryShaderData(int program)
            {
                int tmp;

                GL.GetProgram(program, GetProgramParameterName.GeometryInputType, out tmp);
                GeometryInputType = (PrimitiveType)tmp;

                GL.GetProgram(program, GetProgramParameterName.GeometryOutputType, out tmp);
                GeometryOutputType = (PrimitiveType)tmp;

                GL.GetProgram(program, GetProgramParameterName.GeometryShaderInvocations, out tmp);
                GeometryShaderInvocations = tmp;

                GL.GetProgram(program, GetProgramParameterName.GeometryVerticesOut, out tmp);
                GeometryVerticesOut = tmp;
            }
        }
    }
}
