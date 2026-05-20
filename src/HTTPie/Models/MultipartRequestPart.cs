// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.Models;

public sealed record MultipartTextPart(string FieldName, string Value);

public sealed record FileUploadPart(string FieldName, string FilePath);
